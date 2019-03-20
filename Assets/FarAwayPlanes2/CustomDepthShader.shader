Shader "Unlit/CustomDepthShader"
{
	Properties
	{
        _MaxDist("Maximum distance", float) = 1
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float3 viewpos : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewpos = UnityObjectToViewPos(v.vertex);
				return o;
			}

            float _MaxDist;
			
			fixed4 frag (v2f i) : SV_Target
			{
                /* the world depth (distance to camera) is equal to '-i.viewpos.z' */
                float sitcking_depth = _MaxDist - (-i.viewpos.z);
                return fixed4(sitcking_depth, 0, 0, 1);
			}
			ENDCG
		}
	}
}
