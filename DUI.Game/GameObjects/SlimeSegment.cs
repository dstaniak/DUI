using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>A grid step's endpoints and direction, in logical board pixels.</summary>
public struct SlimeSegment
{
    public Vector2 At;
    public Vector2 To;
    public Vector2 Direction;
    public readonly Vector2 ReverseDirection => new(-Direction.X, -Direction.Y);
}
