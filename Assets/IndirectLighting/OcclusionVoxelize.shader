Shader "Hidden/OcclusionVoxelize" {
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
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile  AXIS_XYZ  AXIS_YZX  AXIS_ZXY
            #include "UnityCG.cginc"

            RWTexture3D<float> RG0;
            int AR_VoxelResolution;

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

            float frag(v2f input) : SV_TARGET
            {
                float3 p = input.pos;
                p.z *= AR_VoxelResolution;
            #if defined(AXIS_YZX)
                int3 coord = int3((int)p.y, (int)p.z, (int)p.x);
            #elif defined(AXIS_ZXY)
                int3 coord = int3((int)p.z, (int)p.x, (int)p.y);
            #else    /* AXIS_XYZ */
                int3 coord = int3((int)p.x, (int)p.y, (int)p.z);
            #endif
                RG0[coord] = 1;
                return 0.0;
            }
            ENDCG
		}
    }
	FallBack Off
}
