Shader "Custom/Controllers" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_SharperColor ("Sharper Color", Range(1, 3)) = 1.0
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BackColor("Back color", Color) = (0,0,0,1)
        _MaskTex("Mask (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        _Cutout ("Cutout", Range(0,1)) = 0.001
	}
	SubShader {
		Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+400"
        }
        LOD 200
        
        Pass {
            /* This pass writes the ghostly blue-ish tint over the whole controller, and sets
               the stencil bit 64 once it is done, in order to avoid setting the alpha color
               multiple times.  This pass is identical to the one in StandardHighVis.shader
               so that it won't set the alpha color multiple times even across the two shaders.
            */
            ZWrite off
            ZTest always
            Stencil {
                Ref 64
                ReadMask 64
                WriteMask 64
                Comp notequal
                Pass replace
            }
            Blend SrcAlpha OneMinusSrcAlpha
            Color (0.2, 0.2, 0.4, 0.4)
        }


        Pass {
            /* This pass draws in plain black the back-facing faces of the controllers that
               are within the _MaskTex.
             */
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            struct appdata {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            fixed4 _BackColor;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                clip(tex2D(_MaskTex, i.uv).r - 0.5);
                return _BackColor;
            }
            ENDCG
        }


        /* The rest adds the standard shader passes that will draw the normal controller,
           with just one twist: the parts that are black in _MaskTex are not rendered at all. 
         */

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
        sampler2D _MaskTex;

		struct Input {
			float2 uv_MainTex;
		};

        fixed _Cutout;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		float _SharperColor;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
            clip(tex2D(_MaskTex, IN.uv_MainTex).r - 0.5);

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed3 base_color = 1.0 - (1.0 - _Color.rgb) * _SharperColor;
			o.Albedo = c.rgb * base_color;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
