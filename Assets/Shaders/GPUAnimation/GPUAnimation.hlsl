#ifndef GPUANIMATION_HLSL_INCLUDED
#define GPUANIMATION_HLSL_INCLUDED

void SampleGPUAnimation_float(
    in float vertexID,
    in float time,
    in float speed,
    in float frameCount,
    in float vertexCount,
    in float instanceOffset,
    in TEXTURE2D_PARAM(animTex, sampler_animTex),
    out float3 outPosOS,
    out float3 outNormalOS,
    out float4 outTangentOS
)
{
    float tripleVertexCount = vertexCount * 3;

    float t = frac(time * speed + instanceOffset);
    float frame = t * frameCount;

    float2 uvPos;
    uvPos.x = (frame + 0.5) / frameCount;
    uvPos.y = (3 * vertexID + 0.5) / tripleVertexCount;

    float2 uvNormal;
    uvNormal.x = uvPos.x;
    uvNormal.y = (3 * vertexID + 1 + 0.5) / tripleVertexCount;

    float2 uvTangent;
    uvTangent.x = uvPos.x;
    uvTangent.y = (3 * vertexID + 2 + 0.5) / tripleVertexCount;

    float4 posData = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvPos, 0);
    outPosOS = posData.xyz;

    float4 normalData = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvNormal, 0);
    outNormalOS = normalData.xyz;

    float4 tangentData = SAMPLE_TEXTURE2D_LOD(animTex, sampler_animTex, uvTangent, 0);
    outTangentOS = tangentData;
}

#endif