using Microsoft.Xna.Framework;

namespace DUI.Graphics;

/// <summary>
/// Maps the fixed 720p frame onto the whole window, without borders.
/// Positions and collision bounds remain in 1280 by 720 game coordinates.
/// </summary>
public sealed class GameDisplay
{
    public const int Width = 1280;
    public const int Height = 720;
    public const int RasterWidth = Width;
    public const int RasterHeight = Height;
    // Keep the compact UI layout while drawing its pixel font at full detail.
    public const int UiScale = 4;
    public const int UiWidth = RasterWidth / UiScale;
    public const int UiHeight = RasterHeight / UiScale;

    public Matrix Transform { get; private set; } = Matrix.Identity;
    public Rectangle OutputBounds { get; private set; } = new(0, 0, RasterWidth, RasterHeight);
    public Matrix ScreenToRaster { get; private set; } = Matrix.Identity;

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // The whole board stays visible without decorative borders or bars.
        // Map each axis independently so drawing and input agree at any size.
        OutputBounds = new Rectangle(0, 0, width, height);
        ScreenToRaster = Matrix.CreateScale(
            RasterWidth / (float)width, RasterHeight / (float)height, 1);
        Transform = Matrix.CreateScale(width / (float)Width, height / (float)Height, 1);
    }

    public Vector2 ToGamePosition(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, ScreenToRaster);
    }
}
