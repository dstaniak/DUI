using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Collision edges of the parking artwork, in logical board pixels.</summary>
internal sealed class ParkingGeometry
{
    public const int TileSize = 80;

    public Rectangle DrivingBounds { get; }
    public Rectangle GateBounds { get; }
    public Rectangle BottleBounds { get; }

    public ParkingGeometry(int columns, int rows)
    {
        // The fence occupies only part of its tile. These offsets follow the
        // inner metal edges in parking-atlas.png at the 80x80 drawing scale.
        const int sideFenceInset = 46;
        const int bottomRailOffset = 26;
        const int gateOffset = 14;
        int fenceRow = (rows - 1) * TileSize;
        int shopBottom = 2 * TileSize;
        int width = columns * TileSize - 2 * sideFenceInset;
        DrivingBounds = new Rectangle(sideFenceInset, shopBottom,
            width, fenceRow + bottomRailOffset - shopBottom);

        // The two middle gate tiles protrude above the regular bottom rail.
        GateBounds = new Rectangle((columns / 2 - 1) * TileSize,
            fenceRow + gateOffset, 2 * TileSize, TileSize - gateOffset);

        // Bottles bounce in a rectangle that also clears the closed gate.
        BottleBounds = new Rectangle(sideFenceInset, shopBottom,
            width, GateBounds.Top - shopBottom);
    }

    public bool IntersectsBoundary(CarPose pose)
    {
        return ConvoyCollision.IsOutsideParking(pose, DrivingBounds) ||
            ConvoyCollision.IntersectsObstacle(pose, GateBounds);
    }
}
