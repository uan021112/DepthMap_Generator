// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthShader"
{
SubShader {
Tags { "RenderType"="Opaque" }
Cull Off 
Pass{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
 
sampler2D _CameraDepthTexture;
 
struct v2f {
   float4 pos : SV_POSITION;
   float4 scrPos:TEXCOORD1;
};
 
//Vertex Shader
v2f vert (appdata_base v){
   v2f o;
   o.pos = UnityObjectToClipPos (v.vertex);
   o.scrPos=ComputeScreenPos(o.pos);
   //for some reason, the y position of the depthtexture comes out inverted
   //o.scrPos.y = 1 - o.scrPos.y;
   return o;
}
 
// half4 pack(float depth)
// {
// 	const half4 bitShift = half4(1.0, 256.0, 256.0 * 256.0, 256.0 * 256.0 * 256.0);
// 	const half4 bitMask = half4(1.0/256.0, 1.0/256.0, 1.0/256.0, 0.0);
// 	half4 rgbaDepth = frac(depth * bitShift);
// 	rgbaDepth -= rgbaDepth.gbaa * bitMask;
// 	return rgbaDepth;
// }

//Fragment Shader
half4 frag (v2f i) : COLOR
{
	float rawDepth = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)).r;
	float depth = Linear01Depth(rawDepth);
	// return pack(depth);
	return half4(depth, depth, depth, 1.0f);
}
ENDCG
}
}
FallBack "Diffuse"
}
