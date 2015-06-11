// Original file ColorCorrection3DLut produced by Unity Technologies.
// Modified by Paulius LIEKIS to work with 2D textures instead of 3D. This 
// allows it to be used on mobiles.

Shader "Hidden/ColorCorrection2DLut" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}		
	}

CGINCLUDE

#include "UnityCG.cginc"

struct v2f {
	float4 pos : POSITION;
	float2 uv  : TEXCOORD0;
};

sampler2D _MainTex;
sampler2D _LutTex;

float _ScaleRG;
float _Offset;
float _Dim;

v2f vert( appdata_img v ) 
{
	v2f o;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.uv =  v.texcoord.xy;	
	return o;
} 

float2 GetUV(float4 c)
{
	//c.g = 1 - saturate(c.g);
	
	float b = floor(c.b * _Dim * _Dim);
	float by = floor(b / _Dim);
	float bx = floor(b - by * _Dim);
	
	float2 uv = c.rg * _ScaleRG + _Offset;
	uv += float2(bx, by) / _Dim;
	
	return uv;
}

float4 frag(v2f i) : COLOR 
{
	//float4 c = tex2D(_LutTex, i.uv * _ScaleRG + _Offset);
	
	float4 c = tex2D(_MainTex, i.uv);
	c.rgb = tex2D(_LutTex, GetUV(c)).rgb;
	//c.g = GetUV(c).y;
	//c.rb = 0;
	//c.b = 0;
	return c;
}

float4 fragLinear(v2f i) : COLOR 
{ 
	float4 c = tex2D(_MainTex, i.uv);
	c.rgb= sqrt(c.rgb);
	c.rgb = tex2D(_LutTex, GetUV(c)).rgb;
	c.rgb = c.rgb*c.rgb; 
	return c;
}

ENDCG 

	
Subshader 
{
	Pass 
	{
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
	  #pragma target 2.0
      ENDCG
  	}

	Pass 
	{
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode off }      

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment fragLinear
	  #pragma target 2.0
      ENDCG
  	}
}

Fallback off
}
