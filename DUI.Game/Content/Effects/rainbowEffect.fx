#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// One complete shift every six seconds of active gameplay.
float Phase;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float hue = input.TextureCoordinates.x + input.TextureCoordinates.y * 0.5 - Phase;
    float3 bands = abs(frac(hue + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    float3 rainbow = saturate(bands - 1.0);
    return float4(rainbow, 0.22) * input.Color;
}

technique Rainbow
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
