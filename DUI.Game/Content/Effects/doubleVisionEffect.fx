#if OPENGL
    #define SV_POSITION POSITION
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
Texture2D GhostTexture;

// UV displacement of the copy's center relative to the window center.
float2 GhostOffset;
float GhostAlpha = 0.1;

sampler2D SpriteTextureSampler : register(s0) = sampler_state
{
    Texture = <SpriteTexture>;
};

// Only the moving copy is filtered, so subpixel motion stays smooth while
// the original view keeps its crisp PointClamp pixel artwork.
sampler2D GhostTextureSampler : register(s1) = sampler_state
{
    Texture = <GhostTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 original = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float2 ghostUv = input.TextureCoordinates - GhostOffset;
    float4 ghost = tex2D(GhostTextureSampler, ghostUv);

    // Outside the shifted image, show the original without stretched edges.
    float inside = step(0.0, ghostUv.x) * step(ghostUv.x, 1.0) *
        step(0.0, ghostUv.y) * step(ghostUv.y, 1.0);
    float3 color = lerp(original.rgb, ghost.rgb, GhostAlpha * ghost.a * inside);
    return float4(color, original.a) * input.Color;
}

technique DoubleVision
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
