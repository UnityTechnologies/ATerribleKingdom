// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Transparent/AlphaBlend Rim"
{
	Properties
	{
		_Color ("Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Texture", 2D) = "white" {}
		
		_RimRamp("Rim", 2D) = "white" {}
		_DepthOffset("Depth Offset", Float) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "IgnoreProjector"="True" "Queue"="Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Offset [_DepthOffset], [_DepthOffset]

			Pass
			{
				Cull Back
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

#pragma target 3.0

				struct appdata
				{
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
					float4 worldPos : TEXCOORD1;
					float4 normal : TEXCOORD2;
				};

				fixed4 _Color;

				sampler2D _MainTex;
				float4 _MainTex_ST;

				sampler2D _RimRamp;
				float4 _RimRamp_ST;
			
				v2f vert (appdata v)
				{
					v2f o;
				
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.normal = normalize(mul(unity_ObjectToWorld, fixed4(v.normal.xyz, 0))); 
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}
			
				fixed4 frag (v2f i) : SV_Target
				{
					// sample the texture
					fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				
					fixed angle = dot(normalize(i.worldPos.xyz - _WorldSpaceCameraPos.xyz), i.normal.xyz) * 0.5f + 0.5f;
					fixed4 rimCol = tex2D(_RimRamp, TRANSFORM_TEX(fixed2(angle, 0.5), _RimRamp));
					col *= rimCol;

					return col;
				}
			ENDCG
		}
	}
}
