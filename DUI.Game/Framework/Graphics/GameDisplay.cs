using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Graphics;

/// <summary>
/// Maps the fixed tutorial board to the screen without changing its geometry.
/// Positions and collision bounds remain in 1280 by 720 game coordinates.
/// </summary>
public sealed class GameDisplay
{
    public const int Width = 1280;
    public const int Height = 720;

    public float Scale { get; private set; } = 1.0f;
    public Vector2 Offset { get; private set; }
    public Matrix Transform { get; private set; } = Matrix.Identity;

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        Scale = Math.Min(width / (float)Width, height / (float)Height);
        Offset = new Vector2((width - Width * Scale) / 2, (height - Height * Scale) / 2);
        Transform = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Offset.X, Offset.Y, 0);
    }

    public Vector2 ToGamePosition(Vector2 screenPosition)
    {
        return (screenPosition - Offset) / Scale;
    }
}
