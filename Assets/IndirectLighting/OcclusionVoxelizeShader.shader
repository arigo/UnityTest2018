Shader "Hidden/OcclusionVoxelizeShader" {
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
			#include "UnityCG.cginc"

			RWTexture3D<uint> RG0;
            int AR_VoxelResolution;

        		
			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

            float frag (v2f input) : SV_TARGET
			{
                int3 coord = int3((int)(input.pos.x), (int)(input.pos.y), (int)(input.pos.z * AR_VoxelResolution));
                RG0[coord] = 1;
				return 0.0;
			}

			ENDCG
		}
	} 
	FallBack Off
}
