// Upgrade NOTE: replaced 'glstate.matrix.modelview[0]' with 'UNITY_MATRIX_MV'
// Upgrade NOTE: replaced 'glstate.matrix.mvp' with 'UNITY_MATRIX_MVP'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader"Cone Stepping" 
{
	Properties 
	{
		
		ambient_color 	("Ambient",  Color) = (0.2, 0.2, 0.2)
		diffuse_color 	("Diffuse",  Color) = (1, 1, 1)
		specular_color 	("Specular", Color) = (0.75,0.75,0.75)
		
		shine 	("Shine", Float) = 32
		tile 	("Tile Factor", Float) = 1
		depth 	("Depth Factor", Float) = 0.1
		step_size_factor 	("Step Size Factor", Float) = 2
				
		color_map 				("color_map", 2D	) = "white" {}
		cone_relief_map 		("cone", 2D			) = "white" {}
		quadcone_relief_map 	("quadcone", 2D		) = "white" {}
		relaxedcone_relief_map 	("relaxedcone", 2D	) = "white" {}
		
		lightpos ("PointLight", Vector) = (-10, 10, -10)
	}
	
	SubShader 	
	{
		Pass
		{
		Cull Back
		AlphaTest GEqual 16
		
CGPROGRAM //-----------
#pragma target 3.0
#pragma vertex 		vertex_shader

//  enable one of them to see the difference
//	#pragma fragment pixel_shader_normal
//	#pragma fragment pixel_shader_relief
//	#pragma fragment pixel_shader_cone
//	#pragma fragment pixel_shader_quadcone
#pragma fragment pixel_shader_relaxedcone 
	
#pragma profileoption MaxTexIndirections=64
#include "UnityCG.cginc"

#define DEPTH_BIAS
#define BORDER_CLAMP

float3 ambient_color;
float3 diffuse_color;
float3 specular_color;

float shine;
float tile;
float depth;
float step_size_factor;

float relief_mode;
float depth_bias;
float border_clamp;

sampler2D color_map;
sampler2D relaxedcone_relief_map;
sampler2D cone_relief_map;
sampler2D quadcone_relief_map;
float3 lightpos;

struct a2v 
{
	float4 vertex 		: POSITION;
	float4 tangent		: TANGENT; 
	float3 normal		: NORMAL; 
	float2 texCoord		: TEXCOORD0; 
};

struct v2f
{
	float4 hpos		: POSITION;
	float3 eye		: TEXCOORD0;
	float3 light	: TEXCOORD1;
	float2 texcoord : TEXCOORD2;
};

v2f vertex_shader(a2v IN) 
{ 
	v2f OUT;
	
	// vertex position IN object space
	float4 pos = float4(IN.vertex.xyz, 1.0);
	
	// vertex position IN clip space
	OUT.hpos 		= UnityObjectToClipPos(pos);
	
	// copy color and texture coordINates
	OUT.texcoord 	= IN.texCoord.xy*tile;;	

	// compute modelview rotation only part
	float4x4 modelviewrot	= float4x4(UNITY_MATRIX_MV);

	// tangent vectors IN view space	
	float3 IN_bINormal 		= cross( IN.normal, IN.tangent.xyz )*IN.tangent.w;	
	float3 tangent			= mul(modelviewrot,IN.tangent.xyz);
	float3 bINormal			= mul(modelviewrot,IN_bINormal.xyz);
	float3 normal			= mul(modelviewrot,IN.normal);
	float3x3 tangentspace	= float3x3(tangent,bINormal,normal);
	
	// vertex position IN view space (with model transformations)
	float3 vpos=mul(UNITY_MATRIX_MV,pos).xyz;
		
	// view and light IN tangent space
	OUT.eye=mul(tangentspace,vpos);
	
	// light position IN tangent space
	OUT.light=mul(tangentspace,lightpos.xyz-vpos);		
	// OUT.light=mul(tangentspace,glstate.light[0].position.xyz -vpos);	
	
	return OUT; 
}

// setup ray pos and dir based on view vector
// and apply depth bias and depth factor
void setup_ray(v2f IN,out float3 p,out float3 v)
{
	p = float3(IN.texcoord,0);
	v = normalize(IN.eye);
	
	v.z = abs(v.z);
	
#ifdef DEPTH_BIAS
		float db = 1.0-v.z; db*=db; db*=db; db=1.0-db*db;
		v.xy *= db;
#endif

	v.xy *= depth;
}

// do normal mapping using given texture coordinate
// tangent space phong lighting with optional border clamp
// normal X and Y stored in red and green channels
float4 normal_mapping(
	sampler2D color_map,
	sampler2D normal_map,
	float2 texcoord,
	v2f IN)
{
	// color map
	float4 color = tex2D(color_map,texcoord);
	
	// normal map
	float4 normal = tex2D(normal_map,texcoord);
	normal.xy = 2*normal.xy - 1;
	normal.y = -normal.y;
	normal.z = sqrt(1.0 - dot(normal.xy,normal.xy));

	// light and view in tangent space
	float3 l = normalize(IN.light);
	float3 v = normalize(IN.eye);

	// compute diffuse and specular terms
	float diff = saturate(dot(l,normal.xyz));
	float spec = saturate(dot(normalize(l-v),normal.xyz));

	// attenuation factor
	float att = 1.0 - max(0,l.z); 
	att = 1.0 - att*att;

	// border clamp
	float alpha=1;	
#ifdef BORDER_CLAMP
	if (texcoord.x<0) 	 alpha=0;
	if (texcoord.y<0) 	 alpha=0;
	if (texcoord.x>tile) alpha=0;
	if (texcoord.y>tile) alpha=0;
#endif

	// compute final color
	float4 finalcolor;
	finalcolor.xyz = ambient_color*color.xyz +
		att*(color.xyz*diffuse_color*diff +
		specular_color*pow(spec,shine));
	finalcolor.w = alpha;
	return finalcolor;
}

#define STEPS_CONE 30
#define STEPS_BINARY 5

// ray intersect depth map using binary cone space leaping
// depth value stored in alpha channel (black is at object surface)
// and cone ratio stored in blue channel
void ray_intersect_relaxedcone(
	sampler2D relaxedcone_relief_map,
	inout float3 p,
	inout float3 v)
{
	float3 p0 = p;

	v /= v.z;
	
    float dist = length(v.xy) * step_size_factor;
	
	[unroll(STEPS_CONE)]
    for (int i = 0; i < STEPS_CONE; i++)
	{
		float4 tex = tex2D(relaxedcone_relief_map, p.xy);

		float height = saturate(tex.w - p.z);
		
		float cone_ratio = tex.z;
		
		p += v * (cone_ratio * height / (dist + cone_ratio));
	}

	v *= p.z*0.5;
	p = p0 + v;

	[unroll(STEPS_BINARY)]
    for (i = 0; i < STEPS_BINARY; i++)
	{
		float4 tex = tex2D(relaxedcone_relief_map, p.xy);
		v *= 0.5;
		if (p.z<tex.w)
			p+=v;
		else
			p-=v;
	}
}

// ray intersect depth map using quad cone space leaping
// depth value stored in alpha channel (black is at object surface)
// and RGBA texture with pyramid ratios for each main direction (N,S,W,E)
void ray_intersect_quadcone(
	sampler2D cone_relief_map,
	sampler2D quadcone_relief_map,
	inout float3 p,
	inout float3 v)
{
	const int num_steps=15;
	
	float2 abs_view = abs(v.xy);
	int index;
	if (abs_view.x>abs_view.y)
		index = v.x<0 ? 1 : 0;
	else
		index = v.y<0 ? 3 : 2;
	
	float dist = length(v.xy);

	for( int i=0;i<num_steps;i++ )
	{
		float4 tex = tex2D(cone_relief_map, p.xy);

		float height = saturate(tex.w - p.z);
		
		//float cone_ratio = tex2D(quadcone_relief_map, p.xy)[index];	// not work in Unity3d ?!
		float cone_ratio; 											
		float4 c = tex2D(quadcone_relief_map, p.xy);
		if(index ==0) {cone_ratio = c[0];}
		if(index ==1) {cone_ratio = c[1];}
		if(index ==2) {cone_ratio = c[2];}
		if(index ==3) {cone_ratio = c[3];}
		
		p += v * (cone_ratio * height / (dist + cone_ratio));
	}
}

#define CONE_NUM_STEPS 20

// ray intersect depth map using cone space leaping
// depth value stored in alpha channel (black is at object surface)
// and cone ratio stored in blue channel
void ray_intersect_cone(
	sampler2D cone_relief_map,
	inout float3 p,
	inout float3 v)
{	
	float dist = length(v.xy);
	
	[unroll(CONE_NUM_STEPS)]
    for (int i = 0; i < CONE_NUM_STEPS; i++)
	{
		float4 tex = tex2D(cone_relief_map, p.xy);
		
		float height = saturate(tex.w - p.z);
		
		float cone_ratio = tex.z;
		
		p += v * (cone_ratio * height / (dist + cone_ratio));
	}
}

#define NUM_STEPS_LIN 30
#define NUM_STEPS_BIN 5

// ray intersect depth map using linear and binary searches
// depth value stored in alpha channel (black at is object surface)
void ray_intersect_relief(
	sampler2D relief_map,
	inout float3 p,
	inout float3 v)
{
    v /= v.z * NUM_STEPS_LIN;
	
	int i;
	// [unroll(NUM_STEPS_LIN)]
    for (i = 0; i < NUM_STEPS_LIN; i++)
	{
		float4 tex = tex2D(relief_map, p.xy);
		if (p.z<tex.w)
			p+=v;
	}
	
	// [unroll(NUM_STEPS_BIN)]
    for (i = 0; i < NUM_STEPS_BIN; i++)
	{
		v *= 0.5;
		float4 tex = tex2D(relief_map, p.xy);
		if (p.z<tex.w)
			p+=v;
		else
			p-=v;
	}
}

float4 pixel_shader_relaxedcone(v2f IN):COLOR
{
	float3 p,v;	
	setup_ray(IN,p,v);
	ray_intersect_relaxedcone(relaxedcone_relief_map,p,v);
	
	// return normal_mapping(color_map,relaxedcone_relief_map,p.xy,IN);
    return tex2D(color_map, p.xy);
}

float4 pixel_shader_quadcone(v2f IN):COLOR
{
	float3 p,v;	
	setup_ray(IN,p,v);
	ray_intersect_quadcone(cone_relief_map,quadcone_relief_map,p,v);
	
	return normal_mapping(color_map,cone_relief_map,p.xy,IN);
}

float4 pixel_shader_cone(v2f IN):COLOR
{
	float3 p,v;	
	setup_ray(IN,p,v);
	ray_intersect_cone(cone_relief_map,p,v);
	
    // return normal_mapping(color_map, cone_relief_map, p.xy, IN);
    return tex2D(color_map, p.xy);
}

float4 pixel_shader_relief(v2f IN):COLOR
{
	float3 p,v;	
	setup_ray(IN,p,v);
	ray_intersect_relief(cone_relief_map,p.xyz,v);
	
    // return normal_mapping(color_map, cone_relief_map, p.xy, IN);
    return tex2D(color_map, p.xy);
}

float4 pixel_shader_normal(v2f IN):COLOR
{
	return normal_mapping(color_map,cone_relief_map,IN.texcoord,IN);
}

ENDCG //----------
		} // Pass 
	} // SubShader 
} // Shader
