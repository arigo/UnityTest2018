Shader "Unlit/minitest2"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
            #pragma target 5.0
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
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
			};


            RWStructuredBuffer<uint> CustomFastNormals : register(u1);
            float4 CustomFastNormals_Size;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = o.vertex.xy / o.vertex.w;
                o.normal = COMPUTE_VIEW_NORMAL;
                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                int2 xy = int2((i.uv + float2(1, 1)) * CustomFastNormals_Size.zw);
                int index = xy.y * int(CustomFastNormals_Size.x) + xy.x;
                float2 norm = EncodeViewNormalStereo(i.normal);
                uint2 norm_int = uint2(norm * 32768);
                CustomFastNormals[index] = norm_int.x * 65536 + norm_int.y;

				return float4(1, i.uv, 1);
			}
			ENDCG
		}
	}
}
