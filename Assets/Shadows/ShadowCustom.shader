Shader "Unlit/ShadowCustom"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "CustomShadows"="Custom" }
		LOD 100

		Pass
		{
			CGPROGRAM
            #pragma target 4.5
            #include "../ShaderDebugger/debugger.cginc"
            #pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float4 shadow_uvzn : TEXCOORD0;
            };

            fixed4 _Color;
            float4x4 ShadowCustomMat;
            Texture2D ShadowCustomTex;
            float3 ShadowCustomNormal;
            SamplerComparisonState samplerShadowCustomTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                float4 world_pos = mul(unity_ObjectToWorld, v.vertex);
                float4 shadow_pos = mul(ShadowCustomMat, world_pos);
                float3 world_normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                float shadow_normal = dot(ShadowCustomNormal, normalize(world_normal));
                o.shadow_uvzn = float4(shadow_pos.xyz / shadow_pos.w, shadow_normal);
				return o;
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;

                float factor = i.shadow_uvzn.w;
                float x;
                if (factor < 0)
                    x = 0;
                else
                    x = factor * UNITY_SAMPLE_SHADOW(ShadowCustomTex, i.shadow_uvzn.xyz);
                x = (x + 1) * 0.5;
                col *= x;

                /*uint root = DebugFragment(i.vertex);
                DbgValue3(root, (float3)ShadowCustomMat[2]);*/

				return col;
			}
			ENDCG
		}
	}
}
