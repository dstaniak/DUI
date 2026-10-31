using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>A car center in board pixels, heading in radians and distance along the head's trail.</summary>
public readonly record struct CarPose(Vector2 Center, float Rotation, double Distance)
{
    public Vector2 Direction => new(MathF.Cos(Rotation), MathF.Sin(Rotation));

    public static CarPose Interpolate(CarPose from, CarPose to, double distance)
    {
        float progress = (float)((distance - from.Distance) / (to.Distance - from.Distance));
        float turn = MathHelper.WrapAngle(to.Rotation - from.Rotation);
        return new CarPose(Vector2.Lerp(from.Center, to.Center, progress),
            from.Rotation + turn * progress, distance);
    }
}
