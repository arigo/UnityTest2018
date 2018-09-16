Shader "Hidden/SunVoxelize" {
    Properties
    {
    }
    SubShader
    {
        Cull Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                return o;
            }

            float4 frag(v2f input) : SV_TARGET
            {
                float4 p = input.pos;
                return float4(ddx(p.x), ddx(p.z), ddx(p.w), 1);
            }
            ENDCG
		}
    }
	FallBack Off
}
