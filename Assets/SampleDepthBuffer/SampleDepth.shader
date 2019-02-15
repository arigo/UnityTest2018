Shader "Custom/SampleDepth"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "SampleDepth"="Block" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 vert (float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			fixed4 frag (float4 pos : SV_POSITION) : SV_Target
			{
				return fixed4(1, 1, 1, 1);
			}
			ENDCG
		}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "SampleDepth"="SampleSurface" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			float4 vert (float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			fixed4 frag (float4 pos : SV_POSITION) : SV_Target
			{
				return fixed4(1, 0, 0, 1);
			}
			ENDCG
		}
	}
}
