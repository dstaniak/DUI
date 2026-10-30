using System;
using Microsoft.Xna.Framework;

namespace DUI.Input;

/// <summary>
/// Recognizes one direction per contact, in game coordinates. A new contact
/// can queue the next turn before the previous turn has been executed.
/// </summary>
public sealed class SwipeTracker
{
    private const float MinimumDistance = 32.0f;
    private Vector2 _origin;
    private bool _tracking;

    public void Begin(Vector2 position)
    {
        // Reserve the top wall for score and pause; ignore letterbox margins.
        _tracking = position.X >= 80 && position.X < 1200 &&
            position.Y >= 80 && position.Y < 640;
        _origin = position;
    }

    public Vector2 Move(Vector2 position)
    {
        Vector2 delta = position - _origin;
        if (!_tracking || delta.LengthSquared() < MinimumDistance * MinimumDistance)
        {
            return Vector2.Zero;
        }

        _tracking = false;
        return Math.Abs(delta.X) > Math.Abs(delta.Y)
            ? new Vector2(Math.Sign(delta.X), 0)
            : new Vector2(0, Math.Sign(delta.Y));
    }

    public void Reset()
    {
        _tracking = false;
    }
}
