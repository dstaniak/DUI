using System;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Independent powerup timers, advanced only by active gameplay time.</summary>
public sealed class PowerupEffects
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

    private TimeSpan _powderRemaining;
    private TimeSpan _mushroomRemaining;
    private long _boostRemainderTicks;

    public bool VisualEffectsSuppressed => _powderRemaining > TimeSpan.Zero;
    public bool ControlsReversed => _mushroomRemaining > TimeSpan.Zero;
    public bool RainbowActive => ControlsReversed && !VisualEffectsSuppressed;

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

    /// <summary>Advances real timers and returns movement time, at 1.2x while powder is active.</summary>
    public TimeSpan Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        // Only the part of this frame covered by powder receives a 20% bonus.
        // Carry fifths of a tick so frame partitioning cannot change distance.
        long boostedTicks = Math.Min(_powderRemaining.Ticks, elapsed.Ticks) + _boostRemainderTicks;
        TimeSpan movementElapsed = elapsed + TimeSpan.FromTicks(boostedTicks / 5);
        _boostRemainderTicks = boostedTicks % 5;

        // Suppression hides visuals without pausing either effect's lifetime.
        _powderRemaining = TimeSpan.FromTicks(Math.Max(0, _powderRemaining.Ticks - elapsed.Ticks));
        _mushroomRemaining = TimeSpan.FromTicks(Math.Max(0, _mushroomRemaining.Ticks - elapsed.Ticks));
        return movementElapsed;
    }

    public void Reset()
    {
        _powderRemaining = TimeSpan.Zero;
        _mushroomRemaining = TimeSpan.Zero;
        _boostRemainderTicks = 0;
    }
}
