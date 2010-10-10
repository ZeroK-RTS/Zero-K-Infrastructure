// These two globals must be present on any shader that 
// is to be used by the attached properties in this project

// This is the source data, the texture or image to shade
sampler2D input : register(s0);

// This is the factor, or amount of shading to apply
float factor : register(c0);

// This is the "main" method taking X,Y coordinates 
// and returning a color
float4 main(float2 uv : TEXCOORD) : COLOR
{
	// Grab the color at XY from the imput
    float4 clr = tex2D(input, uv.xy);
    
    // Calculate the average of the RGB elements
    // This yields a black and white image if
    // this value is applied to RG and B of the color
    float avg = (clr.r + clr.g + clr.b) / 3.0;

	// Set the output color by using the factor global
	// in a way such that if factor is 0, all of the original
	// color is used, otherwise the closer to 1.0 the factor
	// gets the more black and white the color gets
    clr.r = (avg * factor) + (clr.r * (1.0 - factor));
    clr.g = (avg * factor) + (clr.g * (1.0 - factor));
    clr.b = (avg * factor) + (clr.b * (1.0 - factor));

    return clr;
}
        
        
        