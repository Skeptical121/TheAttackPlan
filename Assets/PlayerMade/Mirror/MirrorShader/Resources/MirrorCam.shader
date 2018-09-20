
// Largely based off code from http://www.shaderslab.com/demo-48---alpha-depending-distance-camera.html

Shader "Custom/DarkenedDependingDistance" {

	Properties
	{
    	_MainTex ("Base (RGB)", 2D) = "white" {}
    	// _SurfaceTex ("Base (RGB)", 2D) = "white" {}
    	_Radius ("Radius", Range(0.001, 500)) = 10
    	//_BlendAlpha ("Blend Alpha", float) = 0
	}

    SubShader {

        Pass {

        //Tags { "Queue" = "Transparent" }
         // Blend SrcAlpha OneMinusSrcAlpha
    	// ZTest Always
    	// Blend One One 
     	
        Tags{ "RenderType" = "Opaque" "RenderType" = "Transparent" }
		LOD 200



       	 Material
            {
                Diffuse [_Color]
            }

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                return o;
            }

            float _Radius;

            fixed4 frag (v2f i) : SV_Target
            {


           		fixed4 col = tex2D(_MainTex, i.uv);

            	float dist = length(i.worldPos - _WorldSpaceCameraPos);

				fixed4 other = fixed4(0.01,0.01,0.005,0) * (dist + 9);

				other.a = 1; // Make sure alpha doesn't change

                return other;
            }
            ENDCG

        }
    }
    
}