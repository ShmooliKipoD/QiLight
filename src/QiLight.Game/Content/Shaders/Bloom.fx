#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Parameters
float BloomThreshold = 0.3;
float BloomIntensity = 1.5;
float BloomSaturation = 1.0;
float BaseIntensity = 1.0;
float2 TexelSize;

sampler2D TextureSampler : register(s0);
texture BloomTexture;
sampler2D BloomSampler = sampler_state
{
    Texture = <BloomTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// Vertex shader
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(float4 position : POSITION, float4 color : COLOR0, float2 texCoord : TEXCOORD0)
{
    VertexShaderOutput output;
    output.Position = position;
    output.Color = color;
    output.TexCoord = texCoord;
    return output;
}

// Bright extract pass
float4 BrightExtractPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(TextureSampler, input.TexCoord);
    float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float bright = saturate((luminance - BloomThreshold) / (1.0 - BloomThreshold));
    return color * bright;
}

// Gaussian blur weights (9-tap)
static const int KernelSize = 9;
static const float Weights[9] = { 0.0162, 0.0540, 0.1216, 0.1945, 0.2270, 0.1945, 0.1216, 0.0540, 0.0162 };
static const float Offsets[9] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };

float4 GaussianBlurPS(VertexShaderOutput input) : COLOR
{
    float4 color = float4(0, 0, 0, 0);
    for (int i = 0; i < KernelSize; i++)
    {
        float2 offset = TexelSize * Offsets[i];
        color += tex2D(TextureSampler, input.TexCoord + offset) * Weights[i];
    }
    return color;
}

// Adjust saturation helper
float3 AdjustSaturation(float3 color, float saturation)
{
    float grey = dot(color, float3(0.299, 0.587, 0.114));
    return lerp(float3(grey, grey, grey), color, saturation);
}

// Bloom combine pass
float4 BloomCombinePS(VertexShaderOutput input) : COLOR
{
    float4 baseColor = tex2D(TextureSampler, input.TexCoord);
    float4 bloomColor = tex2D(BloomSampler, input.TexCoord);

    baseColor.rgb = AdjustSaturation(baseColor.rgb, 1.0) * BaseIntensity;
    bloomColor.rgb = AdjustSaturation(bloomColor.rgb, BloomSaturation) * BloomIntensity;

    baseColor.rgb *= (1.0 - saturate(bloomColor));

    return baseColor + bloomColor;
}

technique BrightExtract
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BrightExtractPS();
    }
}

technique GaussianBlur
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL GaussianBlurPS();
    }
}

technique BloomCombine
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL BloomCombinePS();
    }
}
