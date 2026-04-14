#ifndef OUTLINES_HLSL_INCLUDED
#define OUTLINES_HLSL_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif

struct ScharrOperators
{
	float3x3 scharrX;
	float3x3 scharrY;
};

ScharrOperators GetScharrOperators()
{
	ScharrOperators kernels;
	kernels.scharrX = float3x3(
		-3, -10, -3,
		0, 0, 0,
		+3, +10, +3
	);
	kernels.scharrY = float3x3(
		-3, 0, +3,
		-10, 0, +10,
		-3, 0, +3
	);
	return kernels;
}

void ThicknessRound_float(in float thickness, out float outThickness)
{
	if(thickness < 0.5)
	{
		outThickness = 0.25;
	}
	else if(thickness < 0.75)
	{
		outThickness = 0.5;
	}
	else if(thickness < 1)
	{
		outThickness = 0.75;
	}
	else if(thickness < 1.25)
	{
		outThickness = 1;
	}
	else if(thickness < 1.5)
	{
		outThickness = 1.25;
	}
	else if(thickness < 1.75)
	{
		outThickness = 1.5;
	}
	else if(thickness < 2)
	{
		outThickness = 1.75;
	}
	else if(thickness < 2.25)
	{
		outThickness = 2;
	}
	else if(thickness < 2.5)
	{
		outThickness = 2.25;
	}
	else if(thickness < 2.75)
	{
		outThickness = 2.5;
	}
	else if(thickness < 3)
	{
		outThickness = 2.75;
	}
	else if(thickness < 3.25)
	{
		outThickness = 3;
	}
	else
	{
		outThickness = thickness;
	}
}

void DepthBasedOutlines_float(in float2 screenUV, in float2 px, in float thresh, out float outlines)
{
	outlines = 0;
	#ifndef SHADERGRAPH_PREVIEW
	ScharrOperators kernels = GetScharrOperators();
	float           gx = 0;
	float           gy = 0;
	for(int i = -1; i <= 1; i++)
	{
		for(int j = -1; j <= 1; j++)
		{
			if(i == 0 && j == 0) continue;
			float2 offset = float2(i, j) * px;
			float2 uv = screenUV + offset;
			// if(uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
			float depth = SampleSceneDepth(uv);
			gx += depth * kernels.scharrX[i + 1][j + 1];
			gy += depth * kernels.scharrY[i + 1][j + 1];
		}
	}
	outlines = step(thresh, sqrt(gx * gx + gy * gy));
	#endif
}

void NormalBasedOutlines_float(in float2 screenUV, in float2 px, in float thresh, out float outlines)
{
	outlines = 0;
	#ifndef SHADERGRAPH_PREVIEW
	ScharrOperators kernels = GetScharrOperators();
	float           gx = 0;
	float           gy = 0;
	float3          curNormal = SampleSceneNormals(screenUV);
	for(int i = -1; i <= 1; i++)
	{
		for(int j = -1; j <= 1; j++)
		{
			if(i == 0 && j == 0) continue;
			float2 offset = float2(i, j) * px;
			float2 uv = screenUV + offset;
			// if(uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;
			float3 normal = SampleSceneNormals(uv);
			float  d = dot(normal, curNormal);
			gx += d * kernels.scharrX[i + 1][j + 1];
			gy += d * kernels.scharrY[i + 1][j + 1];
		}
	}
	outlines = step(thresh, sqrt(gx * gx + gy * gy));
	#endif
}

void DynamicDepthThreshold_float(in float3 viewDirWS, in float3 normalWS, in float depthThresh, out float thresh)
{
	float nDotV = abs(dot(normalize(normalWS), normalize(viewDirWS)));
	thresh = depthThresh / max(1e-8, smoothstep(0, 1, nDotV));
}

#endif
