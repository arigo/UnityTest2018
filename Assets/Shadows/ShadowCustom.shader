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
            //#include "../ShaderDebugger/debugger.cginc"
            #pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float3 shadow_uvz : TEXCOORD0;
            };

            fixed4 _Color;
            float4x4 ShadowCustomMat;
            Texture2D ShadowCustomTex;
            SamplerComparisonState samplerShadowCustomTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                float4 world_pos = mul(unity_ObjectToWorld, v.vertex);
                float4 shadow_pos = mul(ShadowCustomMat, world_pos);
                o.shadow_uvz = shadow_pos.xyz / shadow_pos.w;
				return o;
			}

			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;

                float x = UNITY_SAMPLE_SHADOW(ShadowCustomTex, i.shadow_uvz);
                x = (x + 1) * 0.5;
                col *= x;

                //uint root = DebugFragment(i.vertex);
                //DbgValue3(root, i.shadow_uvz);

				return col;
			}
			ENDCG
		}
	}
}
