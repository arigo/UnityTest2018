﻿Shader "Custom/StdBouncedLight" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		#pragma target 5.0

		sampler2D _MainTex;

		struct Input {
            float3 worldPos;
            float3 worldNormal;
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        float4x4 AR_OcclusionMatrix;
        float AR_OcclusionVoxelSize;
        sampler3D AR_OcclusionVolume;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = 0;// c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;

            // 1.22475 =~ sqrt(6)/2.  there is a reason
            float3 inpos = IN.worldPos + IN.worldNormal * AR_OcclusionVoxelSize * 1.22475;
            float3 ocpos = mul(AR_OcclusionMatrix, float4(inpos, 1)).xyz;
            float oc = tex3D(AR_OcclusionVolume, ocpos).r * 0.1;
            o.Emission = oc;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
