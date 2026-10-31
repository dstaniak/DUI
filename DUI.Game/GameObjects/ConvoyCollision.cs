using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

internal static class ConvoyCollision
{
    // The 80x80 texture includes transparent padding around the 74x36 body.
    private const float HalfLength = 37f;
    private const float HalfWidth = 18f;

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
        // A fixed circle leaves invisible sides after a turn and catches a cleared wall.
        Vector2 direction = pose.Direction;
        float forwardX = MathF.Abs(direction.X);
        float forwardY = MathF.Abs(direction.Y);
        float extentX = forwardX * HalfLength + forwardY * HalfWidth;
        float extentY = forwardY * HalfLength + forwardX * HalfWidth;

        return pose.Center.X - extentX < bounds.Left || pose.Center.X + extentX > bounds.Right ||
            pose.Center.Y - extentY < bounds.Top || pose.Center.Y + extentY > bounds.Bottom;
    }

    /// <summary>Tests the actual rotated footprint against an axis-aligned obstacle.</summary>
    public static bool IntersectsObstacle(CarPose pose, Rectangle obstacle)
    {
        Vector2 forward = pose.Direction;
        Vector2 side = new(-forward.Y, forward.X);
        Vector2 halfSize = new(obstacle.Width * 0.5f, obstacle.Height * 0.5f);
        Vector2 offset = pose.Center - (new Vector2(obstacle.X, obstacle.Y) + halfSize);
        float forwardX = MathF.Abs(forward.X);
        float forwardY = MathF.Abs(forward.Y);

        // Test all four separating axes. A bounding box alone catches the
        // empty space beside a diagonal car when it passes a gate corner.
        return MathF.Abs(offset.X) < halfSize.X + HalfLength * forwardX + HalfWidth * forwardY &&
            MathF.Abs(offset.Y) < halfSize.Y + HalfLength * forwardY + HalfWidth * forwardX &&
            MathF.Abs(Vector2.Dot(offset, forward)) < HalfLength + halfSize.X * forwardX + halfSize.Y * forwardY &&
            MathF.Abs(Vector2.Dot(offset, side)) < HalfWidth + halfSize.X * forwardY + halfSize.Y * forwardX;
    }
}
