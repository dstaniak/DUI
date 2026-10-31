using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Adds a small visual sway at the same distance along each car's trail.</summary>
internal static class CarSway
{
    private const double PeriodSeconds = 1.2;
    private const float OffsetPixels = 1.5f;
    private const float RotationRadians = MathF.PI / 90f;

    /// <summary>
    /// Returns a shifted center and rotation without changing collision geometry.
    /// </summary>
    public static CarPose GetPose(CarPose pose)
    {
        double phase = pose.Distance / (ConvoyMovement.Speed * PeriodSeconds) * Math.Tau;
        float offset = (float)Math.Sin(phase) * OffsetPixels;
        Vector2 lateral = new(-MathF.Sin(pose.Rotation), MathF.Cos(pose.Rotation));

        // Steering leads sideways displacement by a quarter cycle.
        float rotation = pose.Rotation + (float)Math.Cos(phase) * RotationRadians;
        return new CarPose(pose.Center + lateral * offset, rotation, pose.Distance);
    }
}
