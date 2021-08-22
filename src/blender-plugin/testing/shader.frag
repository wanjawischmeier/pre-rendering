void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    float depth = input0;
    float roughness = input1;
    float transparency = input2;

    depth *= 0xFFFF;
    roughness *= 0xF;
    transparency *= 0xF;

    float roughness << 4;
    // b /= float(0xFF);
    
    fragColor = vec4(0.0, 1.0, 0.0, 1.0);
}