// Shared by the sprite and TMP variants. Positions and radii are screen pixels.
#ifndef SPEECH_BUBBLE_POINTER_FADE_INCLUDED
#define SPEECH_BUBBLE_POINTER_FADE_INCLUDED
float4 _SpeechPointer; // xy: pointer, z: radius
float _SpeechFadeAmount;
float _SpeechMinimumOpacity;
float4 _SpeechProtectedRect0;
float4 _SpeechProtectedRect1;
float4 _SpeechProtectedRect2;
float _SpeechProtectionRoundness;

float SpeechProtectedArea(float2 pixel, float4 rect)
{
    float valid = step(rect.x + 0.001, rect.z) * step(rect.y + 0.001, rect.w);
    float2 halfSize = max((rect.zw - rect.xy) * 0.5, 0);
    float radius = min(halfSize.x, halfSize.y) * _SpeechProtectionRoundness;
    float2 q = abs(pixel - (rect.xy + rect.zw) * 0.5) - halfSize + radius;
    float distanceToEdge = length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
    // A single antialiased edge, not a blurred falloff. Full roundness gives a capsule.
    float aa = max(fwidth(distanceToEdge) * 0.5, 0.5);
    return valid * (1 - smoothstep(-aa, aa, distanceToEdge));
}

float SpeechBubbleOpacity(float4 screenPosition)
{
    float2 pixel = screenPosition.xy / screenPosition.w * _ScreenParams.xy;
    float distanceFromPointer = distance(pixel, _SpeechPointer.xy);
    float aa = max(fwidth(distanceFromPointer) * 0.5, 0.5);
    float hole = 1 - smoothstep(_SpeechPointer.z - aa, _SpeechPointer.z + aa, distanceFromPointer);
    float protection = max(SpeechProtectedArea(pixel, _SpeechProtectedRect0),
        max(SpeechProtectedArea(pixel, _SpeechProtectedRect1),
            SpeechProtectedArea(pixel, _SpeechProtectedRect2)));
    return 1 - hole * (1 - protection) * _SpeechFadeAmount * (1 - _SpeechMinimumOpacity);
}
#endif
