using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace DUI.GameObjects;

/// <summary>One car's curb entry and visual hop, timed by the movement simulation.</summary>
internal sealed class CarHop
{
    private const double DurationSeconds = 0.2;
    private const float FrontOffset = 40f;
    private double _startedAt = double.NegativeInfinity;
    private bool _armed = true;

    public void Advance(CarPose before, CarPose after, TimeSpan elapsed, ReadOnlySpan<Rectangle> islands)
    {
        if (elapsed.TotalSeconds - _startedAt < DurationSeconds)
        {
            return;
        }

        Vector2 frontBefore = before.Center + before.Direction * FrontOffset;
        Vector2 frontAfter = after.Center + after.Direction * FrontOffset;
        if (!IsOnGrass(after.Center, islands) && !IsOnGrass(frontAfter, islands))
        {
            _armed = true;
        }

        bool entersFromAsphalt = _armed && !IsOnGrass(before.Center, islands) &&
            !IsOnGrass(frontBefore, islands) && IsOnGrass(frontAfter, islands);
        if (!entersFromAsphalt)
        {
            return;
        }

        float entry = EntryFraction(frontBefore, frontAfter, islands);
        _startedAt = elapsed.TotalSeconds - ConvoyMovement.StepDuration.TotalSeconds * (1f - entry);
        _armed = false;
    }

    public (float Scale, float Lift) GetTransform(TimeSpan renderTime)
    {
        double age = renderTime.TotalSeconds - _startedAt;
        if (age < 0 || age >= DurationSeconds)
        {
            return (1f, 0f);
        }

        float progress = (float)(age / DurationSeconds);
        float height = progress < 0.5f
            ? Easings.EaseOutCubic(progress * 2f)
            : 1f - Easings.EaseInQuad((progress - 0.5f) * 2f);
        return (1f + 0.28f * height, 12f * height);
    }

    private static bool IsOnGrass(Vector2 point, ReadOnlySpan<Rectangle> islands)
    {
        foreach (Rectangle island in islands)
        {
            if (Contains(island, point))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(Rectangle island, Vector2 point)
    {
        return point.X >= island.Left && point.X < island.Right &&
            point.Y >= island.Top && point.Y < island.Bottom;
    }

    private static float EntryFraction(Vector2 from, Vector2 to, ReadOnlySpan<Rectangle> islands)
    {
        foreach (Rectangle island in islands)
        {
            if (!Contains(island, to))
            {
                continue;
            }

            // The new front is inside this rectangle. The last crossed slab is the curb.
            Vector2 travel = to - from;
            float xEntry = travel.X > 0 ? (island.Left - from.X) / travel.X :
                travel.X < 0 ? (island.Right - from.X) / travel.X : 0f;
            float yEntry = travel.Y > 0 ? (island.Top - from.Y) / travel.Y :
                travel.Y < 0 ? (island.Bottom - from.Y) / travel.Y : 0f;
            return MathHelper.Clamp(MathF.Max(xEntry, yEntry), 0f, 1f);
        }

        return 1f;
    }
}
