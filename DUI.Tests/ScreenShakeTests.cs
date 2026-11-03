using System;
using Assert = DUI.Tests.MovementTests;
using DUI.Graphics;

namespace DUI.Tests;

internal static class ScreenShakeTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Vodka shake swings rapidly through both 24-pixel extremes", FastMotion),
        ("Shake stops exactly at three seconds even with a long update", Duration),
        ("Paused shake keeps its phase and reset clears it", PauseAndReset),
        ("Another vodka restarts the duration and frame partitions agree", RestartAndPartitions)
    ];

    private static void FastMotion()
    {
        var shake = new ScreenShake();
        Assert.Near(0, shake.HorizontalOffset);
        shake.Restart();
        shake.Update(TimeSpan.FromSeconds(1.0 / 72));
        Assert.Near(24, shake.HorizontalOffset);
        shake.Update(TimeSpan.FromSeconds(1.0 / 36));
        Assert.Near(-24, shake.HorizontalOffset);
    }

    private static void Duration()
    {
        var shake = new ScreenShake();
        shake.Restart();
        shake.Update(TimeSpan.FromMilliseconds(2990));
        Assert.Check(Math.Abs(shake.HorizontalOffset) > 1);
        shake.Update(TimeSpan.FromMilliseconds(10));
        Assert.Near(0, shake.HorizontalOffset);
        shake.Restart();
        shake.Update(TimeSpan.FromSeconds(10));
        Assert.Near(0, shake.HorizontalOffset);
    }

    private static void PauseAndReset()
    {
        var shake = new ScreenShake();
        shake.Restart();
        shake.Update(TimeSpan.FromMilliseconds(100));
        float paused = shake.HorizontalOffset;
        for (int frame = 0; frame < 180; frame++)
        {
            Assert.Near(paused, shake.HorizontalOffset);
        }
        shake.Reset();
        Assert.Near(0, shake.HorizontalOffset);
        shake.Update(TimeSpan.FromMilliseconds(100));
        Assert.Near(0, shake.HorizontalOffset);
    }

    private static void RestartAndPartitions()
    {
        var shake = new ScreenShake();
        var split = new ScreenShake();
        shake.Restart();
        shake.Update(TimeSpan.FromMilliseconds(2900));
        shake.Restart();
        split.Restart();
        shake.Update(TimeSpan.FromMilliseconds(1337));
        for (int step = 0; step < 1337; step++)
        {
            split.Update(TimeSpan.FromMilliseconds(1));
        }
        Assert.Near(split.HorizontalOffset, shake.HorizontalOffset);
        Assert.Check(Math.Abs(shake.HorizontalOffset) > 1);
        shake.Update(TimeSpan.FromMilliseconds(1663));
        Assert.Near(0, shake.HorizontalOffset);
    }
}
