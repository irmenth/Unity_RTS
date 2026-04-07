#ifndef CELSHADING_HLSL_INCLUDED
#define CELSHADING_HLSL_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

struct SurfaceInfo
{
	float3 normalWS;
	float3 positionWS;
	float3 viewDirWS;
	bool enableShadow;
	float3 shadowColor;
	bool enableSpecular;
	float3 specularColor;
	float specularThreshold;
	bool enableRim;
	float3 rimColor;
	float rimThreshold;
};

SurfaceInfo CreateSurfaceInfo(
	in float3 normalWS, 
	in float3 positionWS, 
	in float3 viewDirWS, 
	in bool enableShadow, 
	in float3 shadowColor,  
	in bool enableSpecular, 
	in float3 specularColor,
	in float specularThreshold,
	in bool enableRim, 
	in float3 rimColor, 
	in float rimThreshold
	)
{
	SurfaceInfo s;
	s.normalWS = normalWS;
	s.positionWS = positionWS;
	s.viewDirWS = viewDirWS;
	s.enableShadow = enableShadow;
	s.shadowColor = shadowColor;
	s.enableSpecular = enableSpecular;
	s.specularColor = specularColor;
	s.specularThreshold = specularThreshold;
	s.enableRim = enableRim;
	s.rimColor = rimColor;
	s.rimThreshold = rimThreshold;
	return s;
}

void SetShadowCascade(out float thresh[2], out float multiplier[2])
{
	float t[] = {1e-6, 0.5};
	thresh = t;
	float m[] = {1.0, 3.0};
	multiplier = m;
}

#ifndef SHADERGRAPH_PREVIEW
float3 GetSingleLightCelShade(in SurfaceInfo s, in Light l)
{
	float attenuation = l.shadowAttenuation * l.distanceAttenuation;
	float nDotL = dot(s.normalWS, l.direction);
	float3 diffuse = 1;
	if(s.enableShadow)
	{
		diffuse = saturate(nDotL) * attenuation * l.color;
		float diffThresh[2], diffMultiplier[2];
		SetShadowCascade(diffThresh, diffMultiplier);
		for(int i = 0; i < 2; i++)
		{
			float t = diffThresh[i], m = diffMultiplier[i];
			float mask = step(t, diffuse);
			if(mask < 1e-6)
			{
				diffuse = saturate(s.shadowColor * m);
				break;
			}
			if(i == 1)
			{
				diffuse = 1;
			}
		}
	}
	float3 specular = 0;
	if(s.enableSpecular)
	{
		specular = saturate(dot(s.normalWS, normalize(l.direction + s.viewDirWS))) * diffuse;
		specular = step(s.specularThreshold, specular) * s.specularColor;
	}
	float3 rim = 0;
	if(s.enableRim)
	{
		rim = (1 - saturate(dot(s.normalWS, s.viewDirWS))) * diffuse * (nDotL + 1) * 0.5;
		rim = step(s.rimThreshold, rim) * s.rimColor;
	}
	float3 finalColor = diffuse + max(specular, rim);
	return finalColor;
}
#endif

void CelShader_float(
	in float3 normalWS, 
	in float3 positionWS, 
	in float3 viewDirWS,
	in bool enableShadow, 
	in float3 shadowColor, 
	in bool enableSpecular, 
	in float3 specularColor,
	in float specularThreshold, 
	in bool enableRim, 
	in float3 rimColor, 
	in float rimThreshold,
	out float3 finalColors)
{
	finalColors = 1;
	#ifndef SHADERGRAPH_PREVIEW
	SurfaceInfo s = CreateSurfaceInfo(
		normalWS, 
		positionWS, 
		viewDirWS, 
		enableShadow, 
		shadowColor, 
		enableSpecular, 
		specularColor, 
		specularThreshold,
		enableRim, 
		rimColor, 
		rimThreshold
		);
	finalColors = GetSingleLightCelShade(s, GetMainLight(TransformWorldToShadowCoord(positionWS)));
	float additionalLightsCount = GetAdditionalLightsCount();
	if(additionalLightsCount > 0)
	{
		for(int i = 0; i < additionalLightsCount; i++)
		{
			finalColors += GetSingleLightCelShade(s, GetAdditionalLight(i, positionWS));
		}
	}
	#endif
}

#endif
