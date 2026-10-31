using System;
using DUI.Input;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class GameDisplayTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("The 720p frame fills the window without borders or cropping", FullWindow),
        ("UI and steering agree at odd, portrait and small window sizes", InputMapping),
        ("Playable area accepts swipes; the HUD and outside remain excluded", OutsideInput)
    ];

    private static void FullWindow()
    {
        var display = new GameDisplay();
        Assert.Check(GameDisplay.RasterHeight == 720 && GameDisplay.RasterWidth == 1280);
        Assert.Check(GameDisplay.UiWidth * GameDisplay.UiScale == GameDisplay.RasterWidth);
        Assert.Check(GameDisplay.UiHeight * GameDisplay.UiScale == GameDisplay.RasterHeight);

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
            // Gum draws logical UI coordinates with a 4x camera zoom and divides
            // the transformed cursor by that zoom. Match HUD and dialog hits
            // to the world position underneath at every window size.
            foreach (Vector2 ui in new[] { new Vector2(280, 10), new Vector2(65, 115), new Vector2(100, 155) })
            {
                Vector2 raster = ui * GameDisplay.UiScale;
                Vector2 screen = Vector2.Transform(raster, rasterToScreen);
                Assert.Equal(ui, Vector2.Transform(screen, display.ScreenToRaster) / GameDisplay.UiScale);
                Vector2 world = display.ToGamePosition(screen);
                Assert.Equal(raster, world);
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
        display.Resize(1920, 1080);
        Matrix rasterToScreen = Matrix.Invert(display.ScreenToRaster);
        foreach (Vector2 excluded in new[] { new Vector2(400, 20), new Vector2(400, 721),
            new Vector2(-1, 400), new Vector2(1281, 400) })
        {
            var swipe = new SwipeTracker();
            Vector2 start = display.ToGamePosition(Vector2.Transform(excluded, rasterToScreen));
            swipe.Begin(start);
            Assert.Equal(Vector2.Zero, swipe.Move(start + new Vector2(100, 0)));
        }

        var edgeSwipe = new SwipeTracker();
        Vector2 nearBottom = display.ToGamePosition(Vector2.Transform(new(400, 620), rasterToScreen));
        edgeSwipe.Begin(nearBottom);
        Assert.Equal(Vector2.UnitX, edgeSwipe.Move(nearBottom + new Vector2(100, 0)));
    }
}
