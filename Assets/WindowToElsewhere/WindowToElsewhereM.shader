Shader "Custom/WindowToElsewhereM"
{
	Properties
	{
		_LeftTex ("Texture", 2D) = "white" {}
        _RightTex("Texture", 2D) = "white" {}
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
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _LeftTex, _RightTex;
            float4x4 WTETransform2D;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = o.vertex;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float3 uv1 = i.uv.xyw / i.uv.w;
                float2 uv = mul((float3x3)WTETransform2D, uv1);
                fixed4 col;
                if (unity_StereoEyeIndex == 0)
                    col = tex2D(_LeftTex, uv);
                else
                    col = tex2D(_RightTex, uv);
				return col;
			}
			ENDCG
		}
	}
}
