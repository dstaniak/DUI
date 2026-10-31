using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>A grid step's endpoints and direction, in logical board pixels.</summary>
public struct SnakeSegment
{
    public Vector2 At;
    public Vector2 To;
    public Vector2 Direction;
    // The incoming direction travels with the step to keep visual turns continuous.
    public Vector2 PreviousDirection;
    public readonly Vector2 ReverseDirection => new(-Direction.X, -Direction.Y);
}
