Shader "Hidden/iCE/LightStreakFilter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always Fog { Mode Off }

		// Pass 0: Threshold
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment threshold
			#include "LightStreakFilter.cginc"
			ENDCG
		}

		// Pass 1: Streak
		Pass
		{
			CGPROGRAM
			#define FLIP_UV_Y_ON
			#pragma vertex vert_img
			#pragma fragment lightStreak
			#include "LightStreakFilter.cginc"
			ENDCG
		}

		// Pass 2: Compose
		Pass
		{
			Blend One One
			CGPROGRAM
			#define FLIP_UV_Y_ON
			#pragma vertex vert_img
			#pragma fragment compose
			#include "LightStreakFilter.cginc"
			ENDCG
		}

		// Pass 3: Compose
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment flare
			#include "LightStreakFilter.cginc"
			ENDCG
		}

		// Pass 4: Compose
		Pass
		{
			Blend One One
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment final
			#include "LightStreakFilter.cginc"
			ENDCG
		}
	}
}
