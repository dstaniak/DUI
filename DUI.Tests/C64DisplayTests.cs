using System;
using DUI.Input;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class C64DisplayTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("The 200-pixel raster fills the window without borders or cropping", FullWindow),
        ("UI and steering agree at odd, portrait and small window sizes", InputMapping),
        ("Playable area accepts swipes; the HUD and outside remain excluded", OutsideInput)
    ];

    private static void FullWindow()
    {
        var display = new GameDisplay();
        Assert.Check(GameDisplay.RasterHeight == 200 && GameDisplay.RasterWidth == 356);
        Assert.Equal(Vector2.Zero, Vector2.Transform(Vector2.Zero, GameDisplay.RasterTransform));
        Assert.Equal(new(356, 200), Vector2.Transform(new(1280, 720), GameDisplay.RasterTransform));

        foreach (Point size in new[] { new Point(1280, 720), new Point(2560, 1440), new Point(720, 1280) })
        {
            display.Resize(size.X, size.Y);
            Assert.Check(display.OutputBounds == new Rectangle(0, 0, size.X, size.Y));
            Assert.Equal(Vector2.Zero, Vector2.Transform(Vector2.Zero, display.Transform));
            Assert.Equal(new(size.X, size.Y), Vector2.Transform(new(1280, 720), display.Transform));
        }
    }

    private static void InputMapping()
    {
        var display = new GameDisplay();
        foreach (Point size in new[] { new Point(1281, 721), new Point(719, 1279),
            new Point(319, 199), new Point(160, 100), new Point(1, 1) })
        {
            display.Resize(size.X, size.Y);
            Matrix rasterToScreen = Matrix.Invert(display.ScreenToRaster);
            // Cover the HUD, dialog and lower parking, using the exact rectangle
            // sent to SpriteBatch. UI and world input must hit the same pixel.
            foreach (Vector2 raster in new[] { new Vector2(320, 10), new Vector2(65, 115), new Vector2(100, 180) })
            {
                Vector2 screen = Vector2.Transform(raster, rasterToScreen);
                Assert.Equal(raster, Vector2.Transform(screen, display.ScreenToRaster));
                Vector2 world = display.ToGamePosition(screen);
                Assert.Equal(raster, Vector2.Transform(world, GameDisplay.RasterTransform));
                Assert.Equal(screen, Vector2.Transform(world, display.Transform));
            }
        }
        Rectangle previous = display.OutputBounds;
        display.Resize(0, 0);
        Assert.Check(previous == display.OutputBounds);
    }

    private static void OutsideInput()
    {
        var display = new GameDisplay();
        display.Resize(1280, 720);
        Matrix rasterToScreen = Matrix.Invert(display.ScreenToRaster);
        foreach (Vector2 excluded in new[] { new Vector2(100, 5), new Vector2(100, 201),
            new Vector2(-1, 100), new Vector2(357, 100) })
        {
            var swipe = new SwipeTracker();
            Vector2 start = display.ToGamePosition(Vector2.Transform(excluded, rasterToScreen));
            swipe.Begin(start);
            Assert.Equal(Vector2.Zero, swipe.Move(start + new Vector2(100, 0)));
        }

        var edgeSwipe = new SwipeTracker();
        Vector2 nearBottom = display.ToGamePosition(Vector2.Transform(new(100, 170), rasterToScreen));
        edgeSwipe.Begin(nearBottom);
        Assert.Equal(Vector2.UnitX, edgeSwipe.Move(nearBottom + new Vector2(100, 0)));
    }
}
