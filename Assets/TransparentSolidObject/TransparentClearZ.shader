Shader "Unlit/TransparentClearBkgnd"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent-10" }
		LOD 100

		Pass
		{
            ZWrite on
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            float4 vert(appdata v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return UnityObjectToClipPos(v.vertex);
            }

            fixed4 frag(float4 v : SV_POSITION) : SV_Target
            {
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
	}
}
