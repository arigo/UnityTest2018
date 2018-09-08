Shader "Unlit/KbdProjection"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Corner ("Corner", Vector) = (0, 0, 0, 0)
        _XProj("XProj", Vector) = (0, 0, 0, 0)
        _YProj("YProj", Vector) = (0, 0, 0, 0)
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

			sampler2D _MainTex;
            
            float3 _Corner;
            float3 _XProj;
            float3 _YProj;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				
                float4 p4 = mul(unity_ObjectToWorld, v.vertex);
                float3 p3 = p4.xyz / p4.w;
                p3 -= _Corner;
                o.uv.x = dot(p3, _XProj);
                o.uv.y = dot(p3, _YProj);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
