#include "UnityCG.cginc" 

uniform sampler2D _MainTex;
uniform sampler2D _SpectrumTex;
uniform sampler2D _FlareTex;
uniform float4 _MainTex_TexelSize;
uniform float _Gain;
uniform float4 _Direction;
uniform float _Offset;
uniform float _Attenuation;
uniform float _Threshold;
uniform float _Diffraction;
uniform int _Boundary;

// Lens refractions
static const float3 offset[19] =
{
	float3(0.9, 0.01, 4),
	float3(0.7, 0.25, 25),
	float3(0.3, 0.25, 15),
	float3(1, 1.0, 5),
	float3(-0.15, 2, 1),
	float3(-0.3, 2, 1),
	float3(6, 5, 6),
	float3(7, 5, 7),
	float3(8, 5, 8),
	float3(9, 5, 9),
	float3(0.24, 1, 10),
	float3(0.32, 1, 10),
	float3(0.4, 1, 10),
	float3(0.5, -0.5, 2),
	float3(2, 2, -5),
	float3(5, 3, 7),
	float3(20, 0.5, 0),
	float3(0.4, 1, 10),
	float3(0.00001, 10, 20)
};

static const float3 indicies[19] =
{
	float3(0.3, 0.55, 0.2),
	float3(0, 1.5, 0.7),
	float3(0, 0, 1.5),
	float3(0.2, 0.2, 0),
	float3(0.15, 0, 0),
	float3(0, 0, 0.15),
	float3(1.4, 0.25, 0.4), // This is the inner one
	float3(1, 1, 0),
	float3(0, 1, 1),
	float3(0, 0, 1.4),
	float3(0.5, 0.73, 0.5),
	float3(1, 1, 0),
	float3(0.4, 0.2, 0.52),
	float3(0.3, 0.1, 0),
	float3(0, 0, 1),
	float3(0.012,0.313,0.588),
	float3(0.7, 0.5, 0.8), //This is the outer one
	float3(0, 0, 0.2),
 	float3(0.012,0.313,0.588)
};

inline float4 tex2DDiffract(sampler2D tex, float2 uv, float2 offset)
{	
		float3 color = 0;
		float3 distortion = float3(-0.01f, 0.0f, 0.01f);
		color.x = tex2Dlod(tex, float4(uv.xy + offset.xy * distortion.x, 0, 0)).x;
		color.y = tex2Dlod(tex, float4(uv.xy + offset.xy * distortion.y, 0, 0)).y;
		color.z = tex2Dlod(tex, float4(uv.xy + offset.xy * distortion.z, 0, 0)).z;
				
		return float4(color.rgb, 1.0);
}

inline float2 ThresholdBounds(float2 uv, int boundary)
{
		float2 bounds;
		bounds.x = smoothstep(float2(0, 0), boundary, uv * _ScreenParams.xy);
		bounds.y = smoothstep(float2(0, 0), boundary, _ScreenParams.xy - (uv * _ScreenParams.xy));
		return bounds.x * bounds.y;
}

inline float Luma(float3 color)
{
	return dot(color.rgb, 0.333);
}

inline half3 UpsampleFilter(sampler2D tex, float2 uv)
{
    // 9-tap bilinear upsampler (tent filter)
    float4 d = _MainTex_TexelSize.xyxy * float4(-1, -1, +1, +1);

    half3 s;
    s  = (tex2D(tex, uv + d.xy));
    s += (tex2D(tex, uv + d.zy));
    s += (tex2D(tex, uv + d.xw));
    s += (tex2D(tex, uv + d.zw));

    return s * (1.0 / 4);
}

inline float3 LensRefraction(sampler2D col, float2 coords)
{
	float3 lensRefract = float3(0.0f, 0.0f, 0.0f);
	float2 sampleUV = float2(0.0f, 0.0f);

	float2 centerTap = (coords - float2(0.5f, 0.5f));
	centerTap *= float2((_ScreenParams.x / _ScreenParams.y) * 1.15, 1.25f) * 0.75;

	for(int j = 0; j < 19; j++)
	{
		sampleUV = centerTap * offset[j].x;

		sampleUV *= pow(2.0f * length(float2(centerTap.x, centerTap.y)), offset[j].y * 3.5f);
		sampleUV *= offset[j].z;
		sampleUV = float2(0.5f, 0.5f) - sampleUV;

		float2 weightedRay = (sampleUV - float2(0.5f, 0.5f)) * 2.0f;
		float lensRay = saturate(1.0f - dot(weightedRay, weightedRay));
		float3 flare = dot(tex2D(col, sampleUV), 0.333f);
		flare = max(0.0f, flare - 1.9f); // TODO: Make this a public variable
		flare *= indicies[j].rgb * lensRay;

		lensRefract += flare;
	}

	return lensRefract;
}

float4 threshold(v2f_img i) : SV_Target
{
    float4 color = tex2D(_MainTex, i.uv);
		float luma = Luma(color.rgb);

		if (luma < _Threshold) return 0;

		float2 bounds = ThresholdBounds(i.uv, _Boundary);
		return float4(color.rgb * color.a * _Gain * bounds.x * bounds.y, 0);
}

float4 lightStreak(v2f_img i) : SV_Target 
{
		float2 dx = _MainTex_TexelSize.xy;
		float4 color = tex2D(_MainTex, i.uv);

		float attenuationSquared = _Attenuation * _Attenuation;
		float attenuationCubed = _Attenuation * _Attenuation * _Attenuation;

		return color
			+ _Attenuation       * tex2D(_MainTex, i.uv     + _Offset * _Direction.xy * dx)
			+ attenuationSquared * tex2D(_MainTex, i.uv + 2 * _Offset * _Direction.xy * dx)
			+ attenuationCubed   * tex2D(_MainTex, i.uv + 3 * _Offset * _Direction.xy * dx);
}

float4 compose(v2f_img i) : SV_Target 
{
		// float4 spectrum = tex2D(_SpectrumTex, i.uv) * 2.0 * UNITY_PI;
		float4 blurredSpectrum = float4(UpsampleFilter(_SpectrumTex, i.uv), 1);

		float4 streak = tex2DDiffract(_MainTex, i.uv, _Diffraction);
		return lerp(blurredSpectrum * streak, streak, 1.0f - _Diffraction);
}

float4 flare(v2f_img i) : SV_Target
{
	// return tex2D(_MainTex, i.uv);
	return float4(LensRefraction(_MainTex, i.uv), 1.0);
}

float4 final(v2f_img i) : SV_Target
{
	float4 g = tex2D(_MainTex, i.uv);
	float4 l = tex2D(_FlareTex, i.uv);
	return g;
}
