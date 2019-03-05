Shader "Custom/WindowToElsewhere"
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
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = o.vertex;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float2 uv = i.uv.xy / i.uv.w;
                uv = float2(1 + uv.x, 1 - uv.y) * 0.5;
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
