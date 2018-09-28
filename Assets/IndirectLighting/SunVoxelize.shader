Shader "Hidden/SunVoxelize" {
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float normal_dot : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.pos.xyz /= o.pos.w;
                o.pos.w = 1;
                float3 normal3 = mul((float3x3)UNITY_MATRIX_MVP, normal);
                o.normal_dot = normal3.z / length(normal3);
                return o;
            }

            float frag(v2f i) : SV_TARGET
            {
                /*float4 c = 0;
                c.rgb = i.normal * 0.5 + 0.5;
                if (i.normal.z > 0.95)
                    c.rgb = float3(1, 1, 1);*/

                return i.normal_dot;
            }
            ENDCG
		}
    }
	FallBack Off
}
