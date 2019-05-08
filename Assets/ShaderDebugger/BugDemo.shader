Shader "Unlit/BugDemo"
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
            #pragma target 5.0
            #include "debugger.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float4 localPosition : float4;
            };

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPosition = v.vertex;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float red = i.localPosition.x;
				return fixed4(red, 0, 0, 1);
			}
			ENDCG
		}
	}
}
