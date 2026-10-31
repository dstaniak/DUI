#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
float3 Palette[16];

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float3 source = tex2D(SpriteTextureSampler, input.TextureCoordinates).rgb;
    float3 nearest = Palette[0];
    float nearestDistance = 4.0;
    float3 weights = float3(0.30, 0.59, 0.11);
    [unroll]
    for (int i = 0; i < 16; i++)
    {
        float3 difference = source - Palette[i];
        float distance = dot(difference * difference, weights);
        if (distance < nearestDistance)
        {
            nearestDistance = distance;
            nearest = Palette[i];
        }
    }
    // Patterns belong to the artwork; this pass only enforces the palette
    // after blending, UI rendering and the pause fade.
    return float4(nearest, 1.0);
}

technique C64Palette
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
