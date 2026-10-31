using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary.Graphics;

/// <summary>
/// Maps the fixed board through a 320x200 raster to the screen.
/// Positions and collision bounds remain in 1280 by 720 game coordinates.
/// </summary>
public sealed class GameDisplay
{
    public const int Width = 1280;
    public const int Height = 720;
    public const int RasterWidth = 320;
    public const int RasterHeight = 200;
    public const int BorderHeight = 10;
    public const float RasterScale = 0.25f;

    public static Matrix RasterTransform { get; } = Matrix.CreateScale(RasterScale) *
        Matrix.CreateTranslation(0, BorderHeight, 0);

    public float Scale { get; private set; } = RasterScale;
    public Vector2 Offset { get; private set; } = new(0, BorderHeight);
    public Matrix Transform { get; private set; } = RasterTransform;
    public Rectangle OutputBounds { get; private set; } = new(0, 0, RasterWidth, RasterHeight);
    public Matrix ScreenToRaster { get; private set; } = Matrix.Identity;

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        float fit = Math.Min(width / (float)RasterWidth, height / (float)RasterHeight);
        float pixelScale = fit >= 1 ? MathF.Floor(fit) : fit;
        int outputWidth = Math.Max(1, (int)(RasterWidth * pixelScale));
        int outputHeight = Math.Max(1, (int)(RasterHeight * pixelScale));
        OutputBounds = new Rectangle((width - outputWidth) / 2, (height - outputHeight) / 2,
            outputWidth, outputHeight);

        // Derive input mapping from the exact destination rectangle, including
        // rounding on windows smaller than the native raster.
        Matrix rasterToScreen = Matrix.CreateScale(outputWidth / (float)RasterWidth,
            outputHeight / (float)RasterHeight, 1) *
            Matrix.CreateTranslation(OutputBounds.X, OutputBounds.Y, 0);
        ScreenToRaster = Matrix.Invert(rasterToScreen);
        Transform = RasterTransform * rasterToScreen;
        Scale = RasterScale * pixelScale;
        Offset = Vector2.Transform(Vector2.Zero, Transform);
    }

    public Vector2 ToGamePosition(Vector2 screenPosition)
    {
        Vector2 rasterPosition = Vector2.Transform(screenPosition, ScreenToRaster);
        return (rasterPosition - new Vector2(0, BorderHeight)) / RasterScale;
    }
}
