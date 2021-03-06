float4x4 Transform;
float4 Color;
float4 MaskXYWH;


texture2D Tex0;
sampler Tex0Sampler = sampler_state {
    Texture = Tex0;
};

texture2D Tex1;
sampler Tex1Sampler = sampler_state {
    Texture = Tex1;
};


void GetVertex(
    inout float4 position : SV_Position,
    inout float2 texCoord : TEXCOORD0,
    inout float4 color : COLOR0
) {
    position = mul(position, Transform);
}


float4 GetPixel(
    float2 texCoord : TEXCOORD0,
    float4 color : COLOR0
) : SV_Target0 {
    return tex2D(Tex0Sampler, texCoord) * tex2D(Tex1Sampler, MaskXYWH.xy + texCoord * MaskXYWH.zw).a * color * Color;
}


technique Main
{
    pass
    {
        Sampler[0] = Tex0Sampler;
        Sampler[1] = Tex1Sampler;
        VertexShader = compile vs_3_0 GetVertex();
        PixelShader = compile ps_3_0 GetPixel();
    }
}
