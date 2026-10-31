using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

internal static class ConvoyCollision
{
    /// <summary>Tests moving circles in relative space, including contact between tick endpoints.</summary>
    public static bool Intersects(
        Vector2 headBefore, Vector2 headAfter, Vector2 bodyBefore, Vector2 bodyAfter)
    {
        Vector2 relativeStart = headBefore - bodyBefore;
        Vector2 relativeTravel = headAfter - bodyAfter - relativeStart;
        float travelSquared = relativeTravel.LengthSquared();
        float closestTime = travelSquared > 0f
            ? MathHelper.Clamp(-Vector2.Dot(relativeStart, relativeTravel) / travelSquared, 0f, 1f)
            : 0f;
        Vector2 separation = relativeStart + relativeTravel * closestTime;
        const float combinedRadius = 64f;
        return separation.LengthSquared() < combinedRadius * combinedRadius;
    }

    /// <summary>Tests the rotated car footprint against the playable area.</summary>
    public static bool IsOutsideParking(CarPose pose, Rectangle bounds)
    {
        // The 80x80 texture includes transparent padding: the body is about 74x36 pixels.
        // A fixed circle leaves invisible sides after a turn and catches a cleared wall.
        const float halfLength = 37f;
        const float halfWidth = 18f;
        Vector2 direction = pose.Direction;
        float forwardX = MathF.Abs(direction.X);
        float forwardY = MathF.Abs(direction.Y);
        float extentX = forwardX * halfLength + forwardY * halfWidth;
        float extentY = forwardY * halfLength + forwardX * halfWidth;

        return pose.Center.X - extentX < bounds.Left || pose.Center.X + extentX > bounds.Right ||
            pose.Center.Y - extentY < bounds.Top || pose.Center.Y + extentY > bounds.Bottom;
    }
}
