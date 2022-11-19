//UNIT_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void GetCutoutAlpha_float(float2 worldPosition, UnityTexture2D positionLookup, float radius, float lookupSize, float2 uv, float2 texelSize, out float Out)
{
    float minAlpha = 1;
    [loop] for (int i = 0; i < lookupSize; i++)
    {
        float2 lookupUV = float2(i + 0.5 / lookupSize, 0.5);
        float4 position = positionLookup.Sample(SamplerState_Linear_Repeat, lookupUV);

        float2 offset = uv * texelSize;
        float2 origin = worldPosition - offset;
        float2 end = origin + texelSize;

        if (position.x >= origin.x && position.x <= end.x)
        {
            float distance = sqrt(pow(position.x - worldPosition.x, 2) + pow(position.y - worldPosition.y, 2));
            float alpha = clamp(pow(distance,4) / radius, 0, 1);


            minAlpha = min(minAlpha, alpha);
        }
    }

    Out = minAlpha;
}

#endif //MYHLSLINCLUDE_INCLUDED