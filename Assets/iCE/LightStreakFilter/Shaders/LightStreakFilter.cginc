#include "UnityCG.cginc" 

uniform sampler2D _MainTex;
uniform sampler2D _SpectrumTex;
uniform float4 _MainTex_TexelSize;
uniform float _Gain;
uniform float4 _Direction;
uniform float _Offset;
uniform float _Attenuation;
uniform float _Threshold;
uniform float _Diffraction;
uniform int _Boundary;

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
		float4 spectrum = tex2D(_SpectrumTex, i.uv) * 2.0 * UNITY_PI;
		float4 streak = tex2DDiffract(_MainTex, i.uv, _Diffraction);
		return lerp(spectrum * streak, streak, 1.0f - _Diffraction);
}
