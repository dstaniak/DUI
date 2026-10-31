using System;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace DUI.GameObjects;

/// <summary>Computes visual car positions and headings without changing the grid path.</summary>
internal static class CarSway
{
    private const double PeriodSeconds = 1.2;
    private const float OffsetPixels = 5f;
    private const float RotationRadians = MathF.PI / 30f;

    /// <summary>
    /// Returns the shifted top-left grid anchor and rotation in radians.
    /// Add half the car size when drawing a sprite with a centered origin.
    /// </summary>
    public static (Vector2 Position, float Rotation) GetPose(SnakeMovement movement, int segmentIndex)
    {
        SnakeSegment segment = movement.Segments[segmentIndex];
        float progress = movement.MovementProgress;

        float incomingAngle = MathF.Atan2(segment.PreviousDirection.Y, segment.PreviousDirection.X);
        float outgoingAngle = MathF.Atan2(segment.Direction.Y, segment.Direction.X);
        float turnAngle = MathHelper.WrapAngle(outgoingAngle - incomingAngle);
        float heading = incomingAngle + turnAngle * Easings.EaseInOutSine(progress);

        // Each car repeats the head's wave one grid step later than its predecessor.
        double delayedTime = movement.ElapsedTime.TotalSeconds
            - segmentIndex * SnakeMovement.StepDuration.TotalSeconds;
        double phase = delayedTime * Math.Tau / PeriodSeconds;
        float offset = (float)Math.Sin(phase) * OffsetPixels;
        Vector2 lateral = new(-MathF.Sin(heading), MathF.Cos(heading));
        Vector2 position = Vector2.Lerp(segment.At, segment.To, progress) + lateral * offset;

        // Steering leads sideways displacement by a quarter cycle.
        float rotation = heading + (float)Math.Cos(phase) * RotationRadians;
        return (position, rotation);
    }
}
