// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FX/MirrorReflection"
{
	Properties
	{
		// _MirrorCameraPos("_MirrorCameraPos", float3) = (0,0,0)
		_MainTex("Base1 (RGB)", 2D) = "white" {}
		//_DarkMainTex("Base2 (RGB)", 2D) = "white" {}
		[HideInInspector] _ReflectionTex("", 2D) = "white" {}
		[HideInInspector] _DarkenedTex("", 2D) = "white" {}
	}



	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		Pass{
			Material
            {
                Diffuse [_Color]
            }

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
	struct v2f
	{
		float2 uv : TEXCOORD0;
		//float2 uv2 : TEXCOORD1;
		float4 refl : TEXCOORD2;
		float4 pos : SV_POSITION;




		float4 worldPos : TEXCOORD1;
	};

	//struct f2a
    //{
    //    fixed4 col1 : SV_Target0;
     //   fixed4 col0 : SV_Target1;
    //};

	float4 _MainTex_ST;
	//float4 _DarkMainTex_ST;
	v2f vert(appdata_base v) // float4 pos : POSITION, float2 uv : TEXCOORD0
	{
		v2f o;

		o.worldPos = mul(unity_ObjectToWorld, v.vertex);

		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

		//o.uv2 = TRANSFORM_TEX(v.texcoord, _DarkMainTex);

		o.refl = ComputeScreenPos(o.pos);



        // o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
        // o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);




		return o;
	}
	sampler2D _MainTex;
	//sampler2D _DarkMainTex;

	sampler2D _ReflectionTex;
	sampler2D _DarkenedTex;

	fixed4 frag(v2f i) : SV_Target
	{
	            	// We could subtract the distance from the camera to the mirror here, as that value is possible to get

		fixed4 tex = tex2D(_MainTex, i.uv);
		fixed4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i.refl));

		//fixed4 tex2 = tex2D(_DarkMainTex, i.uv2);
		fixed4 refl2 = tex2Dproj(_DarkenedTex, UNITY_PROJ_COORD(i.refl));

		float dist = distance(i.worldPos, _WorldSpaceCameraPos);
		refl2 = 0.05 / (refl2 - fixed4(0.01,0.01,0.005,0) * dist);
		refl2.a = 1;

		refl.rgb = (refl.rgb - 0.5f) * (6) + 0.5f;

		//refl.b *= 1.5;
		//refl.r *= 0.5;
		//refl.g *= 0.5;

		//f2a OUT;
		//OUT.col0 = tex * refl;
		// OUT.col1 = tex2 * refl2;
		//return OUT; //fixed4(0.1,0.1,0.3,0.6) * texref; // return texref;
		return tex * refl * refl2; // tex
	}



		ENDCG
	}

		//Pass {
        //    Blend One One
        //    SetTexture [_DarkenedTex] { combine texture }
        //}
     }

}