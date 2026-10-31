using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace DUI.GameObjects;

/// <summary>A visual hop during a grid step from asphalt onto a grass island.</summary>
internal static class CarHop
{
    private const float PeakScale = 1.28f;
    private const float PeakLiftPixels = 12f;

    /// <summary>
    /// Returns a scale multiplier and upward lift in board pixels.
    /// Endpoints are car centers; progress is the normalized grid-step progress.
    /// </summary>
    public static (float Scale, float Lift) GetTransform(
        Vector2 fromCenter, Vector2 toCenter, float progress, ReadOnlySpan<Rectangle> islands)
    {
        bool entersIsland = !IsOnGrass(fromCenter, islands) && IsOnGrass(toCenter, islands);
        if (!entersIsland)
        {
            return (1f, 0f);
        }

        // Take off as the front reaches the curb, peak when the center crosses it,
        // and land at the next cell center. Sampling the step also freezes on pause.
        float height = progress < 0.5f
            ? Easings.EaseOutCubic(progress * 2f)
            : 1f - Easings.EaseInQuad((progress - 0.5f) * 2f);

        return (1f + (PeakScale - 1f) * height, PeakLiftPixels * height);
    }

    private static bool IsOnGrass(Vector2 center, ReadOnlySpan<Rectangle> islands)
    {
        foreach (Rectangle island in islands)
        {
            if (center.X >= island.Left && center.X < island.Right &&
                center.Y >= island.Top && center.Y < island.Bottom)
            {
                return true;
            }
        }

        return false;
    }
}
