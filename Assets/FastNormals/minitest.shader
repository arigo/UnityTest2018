Shader "Unlit/minitest"
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
			};


            RWStructuredBuffer<uint>	Field	: register(u1);
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = o.vertex.xy / o.vertex.w;
                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                int2 xy = int2(i.uv * 5 + 5);
                int index = xy.y * 10 + xy.x;
                Field[index] = 1;
				return float4(0, i.uv, 1);
			}
			ENDCG
		}
	}
}
