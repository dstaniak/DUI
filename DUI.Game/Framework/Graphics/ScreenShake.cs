using System;

namespace MonoGameLibrary.Graphics;

/// <summary>A three-second horizontal shake, measured in 1280x720 game pixels.</summary>
public sealed class ScreenShake
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);
    private const float Amplitude = 24.0f;
    private const double CyclesPerSecond = 18.0;

    private TimeSpan _elapsed = Duration;

    public TimeSpan RemainingTime => Duration - _elapsed;

    public float HorizontalOffset => _elapsed >= Duration ? 0f :
        Amplitude * (float)Math.Sin(_elapsed.TotalSeconds * CyclesPerSecond * Math.Tau);

    public void Restart()
    {
        _elapsed = TimeSpan.Zero;
    }

    public void Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        if (_elapsed >= Duration)
        {
            return;
        }

        _elapsed += elapsed;
        if (_elapsed > Duration)
        {
            _elapsed = Duration;
        }
    }

    public void Reset()
    {
        _elapsed = Duration;
    }
}
