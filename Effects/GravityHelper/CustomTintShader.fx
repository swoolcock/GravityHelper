#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord)

uniform float Time; // level.TimeActive
uniform float2 CamPos; // level.Camera.Position
uniform float2 Dimensions; // new Vector2(320, 180)
uniform float4x4 TransformMatrix;
uniform float4x4 ViewMatrix;

DECLARE_TEXTURE(text, 0);

// Function to convert RGB to HSV
float3 RGBtoHSV(float3 rgb) {
    float3 hsv;
    float maxC = max(max(rgb.r, rgb.g), rgb.b);
    float minC = min(min(rgb.r, rgb.g), rgb.b);
    float delta = maxC - minC;

    // Value (Brightness)
    hsv.z = maxC;

    // Saturation
    hsv.y = (maxC == 0) ? 0 : delta / maxC;

    // Hue
    if (delta == 0) {
        hsv.x = 0; // Undefined hue
    } else if (maxC == rgb.r) {
        hsv.x = ((rgb.g - rgb.b) / delta) + (rgb.g < rgb.b ? 6 : 0);
    } else if (maxC == rgb.g) {
        hsv.x = ((rgb.b - rgb.r) / delta) + 2;
    } else {
        hsv.x = ((rgb.r - rgb.g) / delta) + 4;
    }
    hsv.x /= 6; // Normalize hue to [0, 1]
    return hsv;
}

float3 HSVtoRGB(float3 input) {
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(input.xxx + K.xyz) * 6.0 - K.www);
    return input.z * lerp(K.xxx, saturate(p - K.xxx), input.y);
}

float4 SpritePixelShader(float2 uv : TEXCOORD0, float4 color : COLOR0, float4 pixelPos : SV_Position) : COLOR
{
    float4 texColor = SAMPLE_TEXTURE(text, uv);
    float3 tintHSV = RGBtoHSV(color.rgb);
    float3 texHSV = RGBtoHSV(texColor.rgb);
    float3 newHSV = float3(tintHSV.x, saturate(texHSV.y * tintHSV.y), saturate(texHSV.z * tintHSV.z));

    return float4(HSVtoRGB(newHSV), texColor.a);
}

void SpriteVertexShader(inout float4 color    : COLOR0,
                        inout float2 texCoord : TEXCOORD0,
                        inout float4 position : SV_Position)
{
    position = mul(position, ViewMatrix);
    position = mul(position, TransformMatrix);
}

technique TestEffect
{
    pass pass0
    {
//        VertexShader = compile vs_3_0 SpriteVertexShader();
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
} 