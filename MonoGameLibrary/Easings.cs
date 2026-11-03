using System;

namespace MonoGameLibrary;

/// <summary>
/// Easing functions for normalized animation progress, matching https://easings.net/.
/// </summary>
/// <remarks>
/// Each function accepts progress from 0 (start) to 1 (end) and returns an
/// interpolation factor. Inputs are not validated or clamped; behavior outside
/// this range is undefined. Back and Elastic curves can return values below 0
/// or above 1, so their results should not be clamped when overshoot is desired.
/// </remarks>
/// <example>
/// <code>
/// float progress = Math.Clamp(elapsedSeconds / durationSeconds, 0f, 1f);
/// float value = start + (end - start) * Easings.EaseOutCubic(progress);
/// </code>
/// </example>
public static class Easings
{
    private const float BackOvershoot = 1.70158f;
    private const float InOutBackOvershoot = BackOvershoot * 1.525f;

    public static float Linear(float progress)
    {
        return progress;
    }

    public static float EaseInSine(float progress)
    {
        return 1f - MathF.Cos(progress * MathF.PI / 2f);
    }

    public static float EaseOutSine(float progress)
    {
        return MathF.Sin(progress * MathF.PI / 2f);
    }

    public static float EaseInOutSine(float progress)
    {
        return (1f - MathF.Cos(progress * MathF.PI)) / 2f;
    }

    public static float EaseInQuad(float progress)
    {
        return progress * progress;
    }

    public static float EaseOutQuad(float progress)
    {
        float remaining = 1f - progress;
        return 1f - remaining * remaining;
    }

    public static float EaseInOutQuad(float progress)
    {
        if (progress < 0.5f)
        {
            return 2f * progress * progress;
        }

        float remaining = 2f * (1f - progress);
        return 1f - remaining * remaining / 2f;
    }

    public static float EaseInCubic(float progress)
    {
        return progress * progress * progress;
    }

    public static float EaseOutCubic(float progress)
    {
        float remaining = 1f - progress;
        return 1f - remaining * remaining * remaining;
    }

    public static float EaseInOutCubic(float progress)
    {
        if (progress < 0.5f)
        {
            return 4f * progress * progress * progress;
        }

        float remaining = 2f * (1f - progress);
        return 1f - remaining * remaining * remaining / 2f;
    }

    public static float EaseInQuart(float progress)
    {
        float squared = progress * progress;
        return squared * squared;
    }

    public static float EaseOutQuart(float progress)
    {
        float remaining = 1f - progress;
        float squared = remaining * remaining;
        return 1f - squared * squared;
    }

    public static float EaseInOutQuart(float progress)
    {
        if (progress < 0.5f)
        {
            float squared = progress * progress;
            return 8f * squared * squared;
        }

        float remaining = 2f * (1f - progress);
        float remainingSquared = remaining * remaining;
        return 1f - remainingSquared * remainingSquared / 2f;
    }

    public static float EaseInQuint(float progress)
    {
        float squared = progress * progress;
        return squared * squared * progress;
    }

    public static float EaseOutQuint(float progress)
    {
        float remaining = 1f - progress;
        float squared = remaining * remaining;
        return 1f - squared * squared * remaining;
    }

    public static float EaseInOutQuint(float progress)
    {
        if (progress < 0.5f)
        {
            float squared = progress * progress;
            return 16f * squared * squared * progress;
        }

        float remaining = 2f * (1f - progress);
        float remainingSquared = remaining * remaining;
        return 1f - remainingSquared * remainingSquared * remaining / 2f;
    }

    public static float EaseInExpo(float progress)
    {
        if (progress == 0f)
        {
            return 0f;
        }

        return MathF.Pow(2f, 10f * progress - 10f);
    }

    public static float EaseOutExpo(float progress)
    {
        if (progress == 1f)
        {
            return 1f;
        }

        return 1f - MathF.Pow(2f, -10f * progress);
    }

    public static float EaseInOutExpo(float progress)
    {
        if (progress == 0f || progress == 1f)
        {
            return progress;
        }

        if (progress < 0.5f)
        {
            return MathF.Pow(2f, 20f * progress - 10f) / 2f;
        }

        return 1f - MathF.Pow(2f, -20f * progress + 10f) / 2f;
    }

    public static float EaseInCirc(float progress)
    {
        return 1f - MathF.Sqrt(1f - progress * progress);
    }

    public static float EaseOutCirc(float progress)
    {
        float remaining = 1f - progress;
        return MathF.Sqrt(1f - remaining * remaining);
    }

    public static float EaseInOutCirc(float progress)
    {
        if (progress < 0.5f)
        {
            float doubled = 2f * progress;
            return (1f - MathF.Sqrt(1f - doubled * doubled)) / 2f;
        }

        float remaining = 2f * (1f - progress);
        return (MathF.Sqrt(1f - remaining * remaining) + 1f) / 2f;
    }

    public static float EaseInBack(float progress)
    {
        // Factoring this way preserves exact endpoints with float arithmetic.
        return progress * progress * (progress + BackOvershoot * (progress - 1f));
    }

    public static float EaseOutBack(float progress)
    {
        float shifted = progress - 1f;
        return 1f + shifted * shifted * (shifted + BackOvershoot * progress);
    }

    public static float EaseInOutBack(float progress)
    {
        if (progress < 0.5f)
        {
            float doubled = 2f * progress;
            return doubled * doubled * (doubled + InOutBackOvershoot * (doubled - 1f)) / 2f;
        }

        float shifted = 2f * progress - 2f;
        return 1f + shifted * shifted * (shifted + InOutBackOvershoot * (shifted + 1f)) / 2f;
    }

    public static float EaseInElastic(float progress)
    {
        if (progress == 0f || progress == 1f)
        {
            return progress;
        }

        float phase = (10f * progress - 10.75f) * (2f * MathF.PI / 3f);
        return -MathF.Pow(2f, 10f * progress - 10f) * MathF.Sin(phase);
    }

    public static float EaseOutElastic(float progress)
    {
        if (progress == 0f || progress == 1f)
        {
            return progress;
        }

        float phase = (10f * progress - 0.75f) * (2f * MathF.PI / 3f);
        return 1f + MathF.Pow(2f, -10f * progress) * MathF.Sin(phase);
    }

    public static float EaseInOutElastic(float progress)
    {
        if (progress == 0f || progress == 1f)
        {
            return progress;
        }

        float phase = (20f * progress - 11.125f) * (2f * MathF.PI / 4.5f);

        if (progress < 0.5f)
        {
            return -MathF.Pow(2f, 20f * progress - 10f) * MathF.Sin(phase) / 2f;
        }

        return 1f + MathF.Pow(2f, -20f * progress + 10f) * MathF.Sin(phase) / 2f;
    }

    public static float EaseInBounce(float progress)
    {
        return 1f - EaseOutBounce(1f - progress);
    }

    public static float EaseOutBounce(float progress)
    {
        const float bounceScale = 7.5625f;
        const float bounceDivisor = 2.75f;

        if (progress < 1f / bounceDivisor)
        {
            return bounceScale * progress * progress;
        }

        if (progress < 2f / bounceDivisor)
        {
            float shifted = progress - 1.5f / bounceDivisor;
            return bounceScale * shifted * shifted + 0.75f;
        }

        if (progress < 2.5f / bounceDivisor)
        {
            float shifted = progress - 2.25f / bounceDivisor;
            return bounceScale * shifted * shifted + 0.9375f;
        }

        float finalShifted = progress - 2.625f / bounceDivisor;
        return bounceScale * finalShifted * finalShifted + 0.984375f;
    }

    public static float EaseInOutBounce(float progress)
    {
        if (progress < 0.5f)
        {
            return (1f - EaseOutBounce(1f - 2f * progress)) / 2f;
        }

        return (1f + EaseOutBounce(2f * progress - 1f)) / 2f;
    }
}
