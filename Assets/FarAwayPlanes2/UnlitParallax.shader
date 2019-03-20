Shader "Unlit/UnlitParallax"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _ParallaxTex ("Parallax Texture", 2D) = "black" {}
        _MinDist ("Minimum distance", float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Pass
		{
            Blend SrcAlpha OneMinusSrcAlpha

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
				float4 vertex : SV_POSITION;
                float3 world_pos : TEXCOORD1;
            };

			sampler2D _MainTex, _ParallaxTex;
            float _MinDist;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.world_pos = (float3)mul(unity_ObjectToWorld, v.vertex);  // -_WorldSpaceCameraPos;
				return o;
			}

            float3 PointAlongEyeLine(float3 world_pos, float d_real)
            {
                float d_far = world_pos.z;
                float3 camera_to_far_point = world_pos - _WorldSpaceCameraPos;
                float z = d_far - d_real;
                camera_to_far_point.xyz *= z / camera_to_far_point.z;
                return world_pos - camera_to_far_point;
            }

            float StickingDepth(float3 pt_3d)
            {
                float2 uv = pt_3d.xy / pt_3d.z;   /* -1..1 */
                uv = uv * 0.5 + 0.5;
                return tex2D(_ParallaxTex, uv).r;
            }

            float3 ClosestPoint(float3 world_pos)
            {
                float d_min = _MinDist;
                float d_max = world_pos.z;

                [unroll] for (int j = 0; j < 40; j++)
                {
                    float d_real = lerp(d_min, d_max, j / 40.0);
                    float3 pt_3d = PointAlongEyeLine(world_pos, d_real);
                    if (StickingDepth(pt_3d) >= d_max - d_real)
                        return pt_3d;
                }
                return world_pos;
            }
			
			fixed4 frag (v2f i) : SV_Target
			{
                float3 real_world_pos = ClosestPoint(i.world_pos);

                float2 uv2 = real_world_pos.xy / real_world_pos.z;   /* -1..1 */
                uv2 = uv2 * 0.5 + 0.5;
                return tex2D(_MainTex, uv2);
			}
			ENDCG
		}
	}
}
