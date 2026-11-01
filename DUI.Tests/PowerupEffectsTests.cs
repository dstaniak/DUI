using System;
using DUI.GameObjects;
using Microsoft.Xna.Framework;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class PowerupEffectsTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Mushroom reverses all four input directions for exactly ten seconds", ReverseDirections),
        ("Powder suppresses visuals for exactly ten seconds", PowderDuration),
        ("Repeated powerups renew rather than toggle or add their duration", RenewDuration),
        ("Powder hides the rainbow without cancelling reversed controls", OverlappingEffects),
        ("Powerups pause, reset and ignore ordinary pickups", PauseAndReset),
        ("Powerup duration is independent of frame partitions", FramePartitions),
        ("Powder increases convoy speed to 1.2x without speeding up timers", MovementBoost),
        ("Powder speed handles expiration within a frame and fractional ticks", PartialBoost),
        ("Mushroom halves convoy speed for ten real seconds", MushroomSlowdown),
        ("Overlapping speed effects multiply and expire independently", OverlappingSpeeds),
        ("Slow movement preserves fractional ticks across frames and reset", PartialSlowdown)
    ];

    private static void ReverseDirections()
    {
        var effects = new PowerupEffects();
        var directions = new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY };
        foreach (Vector2 direction in directions)
        {
            Assert.Check(effects.MapDirection(direction) == direction);
        }
        effects.Collect(PickupType.Mushroom);
        Assert.Check(effects.RainbowActive && !effects.VisualEffectsSuppressed);
        foreach (Vector2 direction in directions)
        {
            Assert.Check(effects.MapDirection(direction) == -direction);
        }
        Assert.Check(effects.MapDirection(Vector2.Zero) == Vector2.Zero);
        effects.Update(PowerupEffects.Duration - TimeSpan.FromTicks(1));
        Assert.Check(effects.ControlsReversed && effects.RainbowActive);
        effects.Update(TimeSpan.FromTicks(1));
        Assert.Check(!effects.ControlsReversed && !effects.RainbowActive);
        Assert.Check(effects.MapDirection(Vector2.UnitX) == Vector2.UnitX);
    }

    private static void PowderDuration()
    {
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Powder);
        Assert.Check(effects.VisualEffectsSuppressed && !effects.ControlsReversed);
        effects.Update(PowerupEffects.Duration - TimeSpan.FromTicks(1));
        Assert.Check(effects.VisualEffectsSuppressed);
        effects.Update(TimeSpan.FromTicks(1));
        Assert.Check(!effects.VisualEffectsSuppressed);
        effects.Collect(PickupType.Powder);
        effects.Update(TimeSpan.FromSeconds(30));
        Assert.Check(!effects.VisualEffectsSuppressed);
    }

    private static void RenewDuration()
    {
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Powder);
        effects.Collect(PickupType.Mushroom);
        effects.Update(TimeSpan.FromSeconds(9));
        effects.Collect(PickupType.Powder);
        effects.Collect(PickupType.Mushroom);
        effects.Update(TimeSpan.FromSeconds(9));
        Assert.Check(effects.VisualEffectsSuppressed && effects.ControlsReversed);
        effects.Update(TimeSpan.FromSeconds(1));
        Assert.Check(!effects.VisualEffectsSuppressed && !effects.ControlsReversed);
    }

    private static void OverlappingEffects()
    {
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Powder);
        effects.Update(TimeSpan.FromSeconds(5));
        effects.Collect(PickupType.Mushroom);
        Assert.Check(effects.ControlsReversed && !effects.RainbowActive);
        effects.Update(TimeSpan.FromSeconds(5));
        Assert.Check(!effects.VisualEffectsSuppressed && effects.RainbowActive);
        effects.Update(TimeSpan.FromSeconds(5));
        Assert.Check(!effects.ControlsReversed && !effects.RainbowActive);

        effects.Collect(PickupType.Mushroom);
        effects.Update(TimeSpan.FromSeconds(5));
        effects.Collect(PickupType.Powder);
        Assert.Check(effects.ControlsReversed && !effects.RainbowActive);
        effects.Update(TimeSpan.FromSeconds(5));
        Assert.Check(!effects.ControlsReversed && effects.VisualEffectsSuppressed);
        effects.Update(TimeSpan.FromSeconds(5));
        Assert.Check(!effects.VisualEffectsSuppressed && !effects.RainbowActive);
    }

    private static void PauseAndReset()
    {
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Powder);
        effects.Collect(PickupType.Mushroom);
        effects.Update(TimeSpan.FromSeconds(9));
        foreach (PickupType type in new[] { PickupType.Beer, PickupType.Vodka, PickupType.Sausage })
        {
            effects.Collect(type);
            effects.Update(TimeSpan.Zero);
            Assert.Check(effects.VisualEffectsSuppressed && effects.ControlsReversed);
        }
        effects.Update(TimeSpan.FromSeconds(1));
        Assert.Check(!effects.VisualEffectsSuppressed && !effects.ControlsReversed);
        effects.Collect(PickupType.Powder);
        effects.Collect(PickupType.Mushroom);
        effects.Reset();
        Assert.Check(!effects.VisualEffectsSuppressed && !effects.ControlsReversed && !effects.RainbowActive);
    }

    private static void FramePartitions()
    {
        var whole = new PowerupEffects();
        var split = new PowerupEffects();
        whole.Collect(PickupType.Powder);
        whole.Collect(PickupType.Mushroom);
        split.Collect(PickupType.Powder);
        split.Collect(PickupType.Mushroom);
        whole.Update(TimeSpan.FromMilliseconds(9999));
        for (int frame = 0; frame < 9999; frame++)
        {
            split.Update(TimeSpan.FromMilliseconds(1));
        }
        Assert.Check(whole.VisualEffectsSuppressed && split.VisualEffectsSuppressed);
        Assert.Check(whole.ControlsReversed && split.ControlsReversed);
        whole.Update(TimeSpan.FromMilliseconds(1));
        split.Update(TimeSpan.FromMilliseconds(1));
        Assert.Check(!whole.VisualEffectsSuppressed && !split.VisualEffectsSuppressed);
        Assert.Check(!whole.ControlsReversed && !split.ControlsReversed);
    }

    private static void MovementBoost()
    {
        var effects = new PowerupEffects();
        var normal = new ConvoyMovement(Vector2.Zero);
        var boosted = new ConvoyMovement(Vector2.Zero);
        boosted.Grow();
        effects.Collect(PickupType.Powder);
        normal.Update(TimeSpan.FromSeconds(1));
        boosted.Update(effects.Update(TimeSpan.FromSeconds(1)));
        Assert.Near(440, normal.Cars[0].Center.X);
        Assert.Near(520, boosted.Cars[0].Center.X);
        Assert.Near(80, Vector2.Distance(boosted.Cars[0].Center, boosted.Cars[1].Center));
        effects.Update(TimeSpan.FromSeconds(8));
        Assert.Check(effects.VisualEffectsSuppressed && !effects.ControlsReversed);
        effects.Update(TimeSpan.FromSeconds(1));
        Assert.Check(!effects.VisualEffectsSuppressed && !effects.ControlsReversed);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(1)) == TimeSpan.FromSeconds(1));
    }

    private static void MushroomSlowdown()
    {
        var effects = new PowerupEffects();
        var movement = new ConvoyMovement(new Vector2(40));
        movement.Grow();
        effects.Collect(PickupType.Mushroom);
        movement.Update(effects.Update(TimeSpan.FromSeconds(1)));
        Assert.Near(240, movement.Cars[0].Center.X);
        Assert.Near(80, Vector2.Distance(movement.Cars[0].Center, movement.Cars[1].Center));
        Assert.Check(effects.MushroomRemaining == TimeSpan.FromSeconds(9));
        Assert.Check(effects.ControlsReversed && effects.RainbowActive);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(9)) == TimeSpan.FromSeconds(4.5));
        Assert.Check(!effects.ControlsReversed && !effects.RainbowActive);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(1)) == TimeSpan.FromSeconds(1));
    }

    private static void OverlappingSpeeds()
    {
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Powder);
        effects.Collect(PickupType.Mushroom);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(1)) == TimeSpan.FromSeconds(0.6));
        Assert.Check(effects.ControlsReversed && effects.VisualEffectsSuppressed && !effects.RainbowActive);

        effects.Reset();
        effects.Collect(PickupType.Powder);
        effects.Update(TimeSpan.FromSeconds(4));
        effects.Collect(PickupType.Mushroom);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(7)) == TimeSpan.FromSeconds(4.1));
        Assert.Check(effects.ControlsReversed && !effects.VisualEffectsSuppressed);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(4)) == TimeSpan.FromSeconds(2.5));

        effects.Reset();
        effects.Collect(PickupType.Mushroom);
        effects.Update(TimeSpan.FromSeconds(4));
        effects.Collect(PickupType.Powder);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(7)) == TimeSpan.FromSeconds(4.8));
        Assert.Check(!effects.ControlsReversed && effects.VisualEffectsSuppressed);
        Assert.Check(effects.Update(TimeSpan.FromSeconds(4)) == TimeSpan.FromSeconds(4.6));
    }

    private static void PartialSlowdown()
    {
        foreach (bool powderActive in new[] { false, true })
        {
            var whole = new PowerupEffects();
            var split = new PowerupEffects();
            foreach (PowerupEffects effects in new[] { whole, split })
            {
                effects.Collect(PickupType.Mushroom);
                if (powderActive)
                {
                    effects.Collect(PickupType.Powder);
                }
            }

            TimeSpan frame = TimeSpan.FromTicks(101);
            TimeSpan expected = whole.Update(frame);
            TimeSpan actual = TimeSpan.Zero;
            for (int tick = 0; tick < frame.Ticks; tick++)
            {
                actual += split.Update(TimeSpan.FromTicks(1));
            }
            Assert.Check(actual == expected && actual.Ticks == (powderActive ? 60 : 50));
            Assert.Check(split.Update(TimeSpan.Zero) == TimeSpan.Zero);
            TimeSpan rest = TimeSpan.FromSeconds(11) - frame;
            TimeSpan wholeRest = whole.Update(rest);
            Assert.Check(wholeRest == split.Update(rest));
            Assert.Check(expected + wholeRest == TimeSpan.FromSeconds(powderActive ? 7 : 6));
        }

        var reset = new PowerupEffects();
        reset.Collect(PickupType.Mushroom);
        Assert.Check(reset.Update(TimeSpan.FromTicks(1)) == TimeSpan.Zero);
        reset.Reset();
        Assert.Check(reset.Update(TimeSpan.FromTicks(1)) == TimeSpan.FromTicks(1));
    }

    private static void PartialBoost()
    {
        var whole = new PowerupEffects();
        var split = new PowerupEffects();
        whole.Collect(PickupType.Powder);
        split.Collect(PickupType.Powder);
        TimeSpan frame = TimeSpan.FromTicks(101);
        TimeSpan expected = whole.Update(frame);
        TimeSpan actual = TimeSpan.Zero;
        for (int tick = 0; tick < frame.Ticks; tick++)
        {
            actual += split.Update(TimeSpan.FromTicks(1));
        }
        Assert.Check(actual == expected && actual.Ticks == 121);
        TimeSpan rest = TimeSpan.FromSeconds(11) - frame;
        TimeSpan wholeRest = whole.Update(rest);
        Assert.Check(wholeRest == split.Update(rest));
        Assert.Check(expected + wholeRest == TimeSpan.FromSeconds(13));

        whole.Reset();
        whole.Collect(PickupType.Powder);
        Assert.Check(whole.Update(TimeSpan.FromSeconds(11)) == TimeSpan.FromSeconds(13));
        Assert.Check(!whole.VisualEffectsSuppressed);
        whole.Collect(PickupType.Powder);
        whole.Update(TimeSpan.FromTicks(4));
        whole.Reset();
        Assert.Check(whole.Update(TimeSpan.FromTicks(1)) == TimeSpan.FromTicks(1));
    }
}
