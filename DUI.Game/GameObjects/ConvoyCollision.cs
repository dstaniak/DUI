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

    public static bool IsOutsideParking(Vector2 center, Rectangle bounds)
    {
        const float radius = 40f;
        return center.X - radius < bounds.Left || center.X + radius > bounds.Right ||
            center.Y - radius < bounds.Top || center.Y + radius > bounds.Bottom;
    }
}
