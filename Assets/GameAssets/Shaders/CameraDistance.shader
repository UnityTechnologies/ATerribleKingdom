// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

//Shader "Custom/CameraDistance" {
//	Properties {
//		_Color ("Color", Color) = (1,1,1,1)
//		_MainTex ("Albedo (RGB)", 2D) = "white" {}
//		_Glossiness ("Smoothness", Range(0,1)) = 0.5
//		_Metallic ("Metallic", Range(0,1)) = 0.0
//	}
//	SubShader {
//		Tags { "RenderType"="Opaque" }
//		LOD 200
//		
//		CGPROGRAM
//		struct v2f
//	    {
//	        half4 pos       : POSITION;
//	        fixed4 color    : COLOR0;
//	        half2 uv        : TEXCOORD0;
//	    };
//
//
//
//	    sampler2D   _MainTex;
//	    half        _MinVisDistance;
//	    half        _MaxVisDistance;
//
//
//
//	    v2f vert (appdata_full v) 
//	    {
//	        v2f o;
//
//	        o.pos = mul((half4x4)UNITY_MATRIX_MVP, v.vertex);
//	        o.uv = v.texcoord.xy;
//	        o.color = v.color;
//	        
//	        //distance falloff
//	        half3 viewDirW = _WorldSpaceCameraPos - mul((half4x4)unity_ObjectToWorld, v.vertex);
//	        half viewDist = length(viewDirW);
//	        half falloff = saturate((viewDist - _MinVisDistance) / (_MaxVisDistance - _MinVisDistance));
//	        o.color.a *= (1.0f - falloff);
//	        return o;
//	    }   
//
//	    fixed4 frag(v2f i) : COLOR 
//	    {
//	        fixed4 color = tex2D(_MainTex, i.uv) * i.color;          
//	        return color;
//	    }
//		ENDCG
//	}
//	FallBack "Diffuse"
//}
