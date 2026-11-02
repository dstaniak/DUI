using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Independent powerup timers, advanced only by active gameplay time.</summary>
public sealed class PowerupEffects
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

    private const int SpeedPrecision = 10;
    private const int PowderSpeedTenths = 12;
    private const int MushroomSpeedTenths = 5;
    private const int CombinedSpeedTenths = PowderSpeedTenths * MushroomSpeedTenths / SpeedPrecision;

    private TimeSpan _powderRemaining;
    private TimeSpan _mushroomRemaining;
    private long _movementRemainderTenths;

    public TimeSpan PowderRemaining => _powderRemaining;
    public TimeSpan MushroomRemaining => _mushroomRemaining;

    public bool VisualEffectsSuppressed => _powderRemaining > TimeSpan.Zero;
    public bool ControlsReversed => _mushroomRemaining > TimeSpan.Zero;
    public bool RainbowActive => ControlsReversed && !VisualEffectsSuppressed;

    /// <summary>The current speed, after collection or expiry, rather than the previous frame's average.</summary>
    public float MovementSpeedMultiplier =>
        (_powderRemaining > TimeSpan.Zero ? PowderSpeedTenths : SpeedPrecision) *
        (_mushroomRemaining > TimeSpan.Zero ? MushroomSpeedTenths : SpeedPrecision) /
        (float)(SpeedPrecision * SpeedPrecision);

    public void Collect(PickupType type)
    {
        if (type == PickupType.Powder)
        {
            _powderRemaining = Duration;
        }
        else if (type == PickupType.Mushroom)
        {
            _mushroomRemaining = Duration;
        }
    }

    public Vector2 MapDirection(Vector2 direction)
    {
        // Map the shared input result so keyboard, gamepad and swipes agree.
        return ControlsReversed ? -direction : direction;
    }

    /// <summary>Advances real timers; movement runs at 1.2x for powder, 0.5x for mushroom, 0.6x for both.</summary>
    public TimeSpan Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        long powderTicks = Math.Min(_powderRemaining.Ticks, elapsed.Ticks);
        long mushroomTicks = Math.Min(_mushroomRemaining.Ticks, elapsed.Ticks);
        long overlapTicks = Math.Min(powderTicks, mushroomTicks);
        long normalTicks = elapsed.Ticks - Math.Max(powderTicks, mushroomTicks);

        // Split at both expirations, even within one frame. Multipliers combine:
        // powder 12/10, mushroom 5/10, both 6/10. Carry tenths of a tick so
        // frame partitioning cannot change distance or lose fractional movement.
        long scaledTenths = (powderTicks - overlapTicks) * PowderSpeedTenths +
            (mushroomTicks - overlapTicks) * MushroomSpeedTenths +
            overlapTicks * CombinedSpeedTenths + _movementRemainderTenths;
        TimeSpan movementElapsed = TimeSpan.FromTicks(checked(normalTicks + scaledTenths / SpeedPrecision));
        _movementRemainderTenths = scaledTenths % SpeedPrecision;

        // Suppression hides visuals without pausing either effect's lifetime.
        _powderRemaining = TimeSpan.FromTicks(Math.Max(0, _powderRemaining.Ticks - elapsed.Ticks));
        _mushroomRemaining = TimeSpan.FromTicks(Math.Max(0, _mushroomRemaining.Ticks - elapsed.Ticks));
        return movementElapsed;
    }

    public void Reset()
    {
        _powderRemaining = TimeSpan.Zero;
        _mushroomRemaining = TimeSpan.Zero;
        _movementRemainderTenths = 0;
    }
}
