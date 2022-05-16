float4x4 Transform;
float4 Color;
float4 MinMax;
float3 Noise;


texture2D Tex0;
sampler Tex0Sampler = sampler_state {
    Texture = Tex0;
};


void GetVertex(
    inout float4 position : SV_Position,
    inout float2 texCoord : TEXCOORD0,
    inout float4 color : COLOR0
) {
    position = mul(position, Transform);
}


// https://gamedev.stackexchange.com/a/32688
float2 rand_2_10(in float2 uv) {
    float noiseX = (frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453));
    float noiseY = sqrt(1 - noiseX * noiseX);
    return float2(noiseX, noiseY);
}


float4 GetPixel(
    float2 texCoord : TEXCOORD0,
    float4 color : COLOR0
) : SV_Target0{
    float4 c1 = tex2D(Tex0Sampler, texCoord);
    float4 c2 = tex2D(Tex0Sampler, clamp(texCoord + sin(rand_2_10(texCoord) * 8.0) * Noise.xy, MinMax.xy, MinMax.zw));
    return lerp(c1, c2, Noise.z) * color * Color;
}


technique Main
{
    pass
    {
        Sampler[0] = Tex0Sampler;
        VertexShader = compile vs_3_0 GetVertex();
        PixelShader = compile ps_3_0 GetPixel();
    }
}
