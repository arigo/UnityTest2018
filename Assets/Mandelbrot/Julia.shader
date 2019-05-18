Shader "Unlit/Julia"
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
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

            StructuredBuffer<float> Palette;
            float4 C;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = (v.uv - float2(0.5, 0.5)) * 5;
				return o;
			}
			
			float4 frag (v2f ii) : SV_Target
			{
                float2 z = ii.uv;
                //float2 z = float2(0, 0);
                //return float4(z.x, z.y, 0, 1);

                for (int i = 0; i < 85 * 2; i += 85)
                {
                    for (int j = 0; j < 85; j++)
                    {
                        /*  z => z * z + c  */
                        z = float2(z.x * z.x - z.y * z.y, z.x * z.y * 2) + C.xy;
                        
                        if (dot(z, z) >= 4)
                        {
                            int k = (i + j) * 3;
                            return float4(Palette[k], Palette[k + 1], Palette[k + 2], 1.0);
                        }
                    }
                }
                return float4(0, 0, 0, 1);
			}
			ENDCG
		}
	}
}
