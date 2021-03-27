using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
[RequireComponent(typeof(Camera))]
public class LightStreakFilter : MonoBehaviour 
{
	public enum RenderQuality
	{
		High = 1,
		Medium = 2,
		Low = 3,
	}

	public RenderQuality quality = RenderQuality.High;

	[Range(2, 16)]
	public int streaks = 8;

	[Range(1, 30)]
	public int power = 7;

	public float strength = 0.1f;

	[Range(0.0f, 1.0f)]
	public float threshold = 1.0f;

	[Range(0.9f, 0.98f)]
	public float attenuation = 0.95f;

	[Range(0.0f, 360.0f)]
	public float angle = 45f;

	[Range(0.0f, 0.35f)]
	public float diffraction = 0.1f;

	public bool rotateBasedOnCamera = true;

	public Texture spectrumTexture;

	private RenderTexture glareRenderTexture;
	private Material glareMaterial;
  private Shader glareShader;
	private Camera currentCamera;
  private enum RenderPass
  {
    Threshold = 0,
		GenerateGlare = 1,
    Compose = 2,
  }

	private void OnEnable ()
	{
			glareShader = Shader.Find("Hidden/iCE/LightStreakFilter");
			
			if (!glareMaterial)
			{
				glareMaterial = new Material(glareShader);
				glareMaterial.hideFlags = HideFlags.HideAndDontSave;
			}

			if (!currentCamera)
			{
				currentCamera = GetComponent<Camera>();
			}

			if (!spectrumTexture)
			{
				spectrumTexture = Resources.Load("SpectrumTexture") as Texture;
			}
	}

	private void OnValidate ()
	{
			strength = Mathf.Max(strength, 0);
	}

	private float CameraRotationAngle ()
	{
			return currentCamera.transform.rotation.y;
	}

	private void Swap<T> (ref T first, ref T second)
	{
			T temp = first;
			first = second;
			second = temp;
	}

	private RenderTexture HalfResolution (RenderTexture source, FilterMode filterMode = FilterMode.Bilinear)
	{
			int halfWidth = source.width >> 1;
			int halfHeight = source.height >> 1;
			RenderTexture downsample = RenderTexture.GetTemporary(halfWidth, halfHeight, 0, source.format);

			downsample.filterMode = filterMode;

			Graphics.Blit(source, downsample);

			return downsample;
	}

	private RenderTexture BuildMipPyrimid (RenderTexture source, int mipLevel)
	{
			RenderTexture mipChain = source;

			if (mipLevel > 0) 
			{
				mipChain = HalfResolution(source);
				for (int i = 1; i < mipLevel; i++) 
				{
					RenderTexture currentMip = HalfResolution(mipChain);
					RenderTexture.ReleaseTemporary(mipChain);
					mipChain = currentMip;
				}
			}

			return mipChain;
	}

	private void LinkStreakParameters (float offset, float streakAttenuation, float streakDirection)
	{
			if (spectrumTexture) 
			{
					glareMaterial.SetTexture("_SpectrumTex", spectrumTexture);
			}

			glareMaterial.SetFloat("_Offset", offset);
			glareMaterial.SetFloat("_Attenuation", streakAttenuation);
			glareMaterial.SetVector("_Direction", new Vector4(Mathf.Cos(streakDirection), Mathf.Sin(streakDirection), 0, 0));
	}

	private void LinkGlowParameters (float glowAmount, float glowThreshold, float diffraction, int power)
	{
			glareMaterial.SetFloat("_Gain", glowAmount);
			glareMaterial.SetFloat("_Threshold", glowThreshold);
			glareMaterial.SetFloat("_Diffraction", diffraction);
			glareMaterial.SetInt("_Boundary", power * 4);
	}

	private void ReleaseGlareTexture ()
	{
			if (glareRenderTexture)
			{
				if (Application.isPlaying)
				{
					Destroy(glareRenderTexture);
				}
				
				DestroyImmediate(glareRenderTexture);
			}
	}

	private void ClearGlareTexture (RenderTexture target)
	{
			RenderTexture old = RenderTexture.active;
			RenderTexture.active = target;
			GL.Clear(true, true, Color.clear);
			RenderTexture.active = old;
	}

	private void GenerateGlareTexture (int width, int height)
	{
			if (glareRenderTexture == null || glareRenderTexture.width != width || glareRenderTexture.height != height)
			{
				ReleaseGlareTexture();
				glareRenderTexture = new RenderTexture(width, height, 24);
				glareRenderTexture.antiAliasing = (QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing);
				glareRenderTexture.filterMode = FilterMode.Bilinear;
				glareRenderTexture.wrapMode = TextureWrapMode.Clamp;
			}

			ClearGlareTexture(glareRenderTexture);
	}

	private void RenderGlare (RenderTexture source, RenderTexture destination) 
	{
			RenderTexture streakX = RenderTexture.GetTemporary(source.width, source.height, 0, source.format, RenderTextureReadWrite.Linear);
			RenderTexture streakY = RenderTexture.GetTemporary(source.width, source.height, 0, source.format, RenderTextureReadWrite.Linear);

			// How many cycles, angular rotations, before a streak is generated
			float period = (2.0f * Mathf.PI) / streaks;

			float cameraRotation = 1.0f;

			if (rotateBasedOnCamera)
			{
				cameraRotation = CameraRotationAngle() * 4.0f * Mathf.PI;
			}


			for (var i = 0; i < streaks; i++) 
			{
				float streakDirection = i * period + (angle * cameraRotation) * Mathf.Deg2Rad;

				// Tell Unity we don't need this - we just need a dummy render texture for the ping pong buffer effect
				streakX.DiscardContents();
				Graphics.Blit(source, streakX, glareMaterial, (int)RenderPass.Threshold);

				RenderLightStreak(ref streakX, ref streakY, streakDirection);

				Graphics.Blit(streakX, destination, glareMaterial, (int)RenderPass.Compose);
			}

			RenderTexture.ReleaseTemporary(streakX);
			RenderTexture.ReleaseTemporary(streakY);
	}

	private void RenderLightStreak (ref RenderTexture streakX, ref RenderTexture streakY, float streakDirection)
	{
		for (var i = 0; i < power; i++) 
		{
				int texelOffset = 1;

				for (var j = 0; j < i; j++)
				{
					texelOffset *= 4;
				}

				float streakAttenuation = Mathf.Pow(attenuation, texelOffset);

				// Link CPU parameters with the GPU shader
				LinkStreakParameters(texelOffset, streakAttenuation, streakDirection);

				streakY.DiscardContents();
				Graphics.Blit(streakX, streakY, glareMaterial, (int)RenderPass.GenerateGlare);

				Swap(ref streakX, ref streakY);
		}
	}

	private void OnRenderImage (RenderTexture source, RenderTexture destination) 
	{
		if (glareMaterial)
		{
			// Downsample based on quality settings
			RenderTexture downsample = BuildMipPyrimid(source, (int)quality);

			// Initialize the glare texture
			GenerateGlareTexture(source.width, source.height);

			// Link CPU parameters with the GPU shader
			LinkGlowParameters(strength, threshold, diffraction, power);

			// Render the glare effect
			RenderGlare(downsample, glareRenderTexture);

			// Add the effect to the frame buffer, clear the framebuffer first as we are doing a blend
			RenderTexture flare = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);

			Graphics.Blit(source, destination);
			Graphics.Blit(glareRenderTexture, destination, glareMaterial, (int)RenderPass.Compose);

			
			Graphics.Blit(source, flare, glareMaterial, 3);

			glareMaterial.SetTexture("MainTex", source);
			glareMaterial.SetTexture("_FlareTex", flare);
			Graphics.Blit(flare, destination, glareMaterial, 4);

			RenderTexture.ReleaseTemporary(flare);

			if ((int)quality > 0) RenderTexture.ReleaseTemporary(downsample);
		}
	}

  private void OnDestroy ()
  {
			ReleaseGlareTexture();
  }
}
