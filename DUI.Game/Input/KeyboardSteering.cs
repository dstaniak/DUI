using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DUI.Input;

/// <summary>Buffers steering key presses in event order, including taps between updates.</summary>
public sealed class KeyboardSteering
{
    private const int Capacity = 8;
    private static readonly Keys[] s_keys =
        [Keys.Up, Keys.W, Keys.Down, Keys.S, Keys.Left, Keys.A, Keys.Right, Keys.D];
    private readonly Queue<Vector2> _directions = new(Capacity);
    private readonly HashSet<Keys> _held = new();

    public void KeyDown(Keys key)
    {
        Vector2 direction = key switch
        {
            Keys.Up or Keys.W => -Vector2.UnitY,
            Keys.Down or Keys.S => Vector2.UnitY,
            Keys.Left or Keys.A => -Vector2.UnitX,
            Keys.Right or Keys.D => Vector2.UnitX,
            _ => Vector2.Zero
        };

        // OS auto-repeat must not create more turns while a key remains held.
        if (direction != Vector2.Zero && _held.Add(key) && _directions.Count < Capacity)
        {
            _directions.Enqueue(direction);
        }
    }

    public void KeyUp(Keys key)
    {
        _held.Remove(key);
    }

    public bool TryReadDirection(out Vector2 direction)
    {
        return _directions.TryDequeue(out direction);
    }

    /// <summary>Discards pending turns and synchronizes held keys after pause or focus changes.</summary>
    public void Reset(KeyboardState state)
    {
        _directions.Clear();
        _held.Clear();
        foreach (Keys key in s_keys)
        {
            if (state.IsKeyDown(key))
            {
                _held.Add(key);
            }
        }
    }
}
