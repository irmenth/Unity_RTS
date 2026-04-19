#ifndef GPUANIMATION_HLSL_INCLUDED
#define GPUANIMATION_HLSL_INCLUDED

struct AnimateData
{
    float frame1;
    float frame2;
    float t;
    float _;
};

StructuredBuffer<AnimateData> _AnimateDataBuffer;

void SampleGPUAnimation_float(
    in float instanceID,
    in float vertexID,
    in float totalFrameCount,
    in float vertexCount,
    in TEXTURE2D_PARAM(animTex, sampler_animTex),
    out float3 outPosOS,
    out float3 outNormalOS
)
{
    AnimateData data = _AnimateDataBuffer[instanceID];
    float doubleCount = vertexCount * 2;

    float2 uvPos1;
    uvPos1.x = (data.frame1 + 0.5) / totalFrameCount;
    uvPos1.y = (2 * vertexID + 0.5) / doubleCount;
    float2 uvPos2;
    uvPos2.x = (data.frame2 + 0.5) / totalFrameCount;
    uvPos2.y = uvPos1.y;

    float2 uvNormal1;
    uvNormal1.x = uvPos1.x;
    uvNormal1.y = (2 * vertexID + 1 + 0.5) / doubleCount;
    float2 uvNormal2;
    uvNormal2.x = uvPos2.x;
    uvNormal2.y = uvNormal1.y;

    float4 posData1 = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvPos1, 0);
    float4 posData2 = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvPos2, 0);
    float4 posData = lerp(posData1, posData2, data.t);
    outPosOS = posData.xyz;

    float4 normalData1 = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvNormal1, 0);
    float4 normalData2 = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvNormal2, 0);
    float4 normalData = normalize(lerp(normalData1, normalData2, data.t));
    outNormalOS = normalData.xyz;
}

#endif