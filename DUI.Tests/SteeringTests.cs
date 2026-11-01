using System;
using DUI.GameObjects;
using DUI.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class SteeringTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Late turns start immediately and return to the intended lane without teleporting", GraceEntry),
        ("Grace turns preserve follower history, spacing and frame independence", GraceHistory),
        ("Keyboard taps between updates keep event order through a two-turn maneuver", KeyboardOrder),
        ("Mushroom maps every buffered keyboard direction in order", KeyboardReversed),
        ("Held keyboard keys do not repeat turns and work again after release", KeyboardRepeat),
        ("Reset clears buffered turns and resynchronizes keys after lost focus", KeyboardReset)
    ];

    private static void GraceEntry()
    {
        foreach (int overshoot in new[] {2, 10, 20})
        {
            foreach (Vector2 turn in new[] {Vector2.UnitY, -Vector2.UnitY})
            {
                var movement = new ConvoyMovement(new Vector2(360));
                movement.Update(TimeSpan.FromMilliseconds(100 + overshoot * 2.5));
                CarPose before = movement.Cars[0];
                CarPose rendered = movement.GetRenderPose(0);
                Assert.Check(movement.QueueDirection(turn));
                Assert.EqualPose(before, movement.Cars[0]);
                Assert.EqualPose(rendered, movement.GetRenderPose(0));
                movement.Update(ConvoyMovement.StepDuration);
                Assert.Check(Vector2.Dot(movement.Cars[0].Direction, turn) > 0);
                Assert.Check(movement.Cars[0].Center.X > before.Center.X);
                Assert.Check(Vector2.Distance(before.Center, movement.Cars[0].Center) <= 2.001f);
                for (int step = 0; step < 60; step++)
                {
                    before = movement.Cars[0];
                    movement.Update(ConvoyMovement.StepDuration);
                    Assert.Near(2, movement.Cars[0].Distance - before.Distance);
                    Assert.Check(Vector2.Distance(before.Center, movement.Cars[0].Center) <= 2.001f);
                }
                Assert.Equal(turn, movement.Cars[0].Direction);
                Assert.Near(440, movement.Cars[0].Center.X);
                Assert.Near(440, movement.GetRenderPose(0).Center.X);
            }
        }
    }

    private static void GraceHistory()
    {
        var small = new ConvoyMovement(new Vector2(360));
        var large = new ConvoyMovement(new Vector2(360));
        foreach (var movement in new[] {small, large})
        {
            movement.Grow();
            movement.Grow();
            movement.Update(TimeSpan.FromMilliseconds(150));
            movement.QueueDirection(Vector2.UnitY);
            movement.QueueDirection(-Vector2.UnitX);
        }
        var history = new CarPose[201];
        history[0] = small.Cars[0];
        for (int step = 1; step < history.Length; step++)
        {
            small.Update(ConvoyMovement.StepDuration);
            history[step] = small.Cars[0];
            for (int car = 1; car < small.Cars.Count; car++)
            {
                Assert.Near(car * 80, small.Cars[0].Distance - small.Cars[car].Distance);
                int earlier = step - car * 40;
                if (earlier >= 0)
                {
                    Assert.EqualPose(history[earlier], small.Cars[car]);
                }
            }
        }
        large.Update(TimeSpan.FromSeconds(1));
        Assert.Check(!small.HasStopped && !large.HasStopped);
        Assert.Near(440, small.Cars[0].Center.Y);
        for (int car = 0; car < small.Cars.Count; car++)
        {
            Assert.EqualPose(small.Cars[car], large.Cars[car]);
            Assert.EqualPose(small.GetRenderPose(car), large.GetRenderPose(car));
        }
        CarPose paused = small.GetRenderPose(0);
        small.Update(TimeSpan.Zero);
        Assert.EqualPose(paused, small.GetRenderPose(0));
    }

    private static void KeyboardOrder()
    {
        foreach (Keys[] keys in new[] {new[] {Keys.Down, Keys.Left}, new[] {Keys.S, Keys.A}})
        {
            var input = new KeyboardSteering();
            var movement = new ConvoyMovement(new Vector2(360));
            foreach (Keys key in keys)
            {
                input.KeyDown(key);
                input.KeyUp(key);
            }
            Assert.Check(input.TryReadDirection(out Vector2 first));
            Assert.Equal(Vector2.UnitY, first);
            Assert.Check(movement.QueueDirection(first));
            Assert.Check(input.TryReadDirection(out Vector2 second));
            Assert.Equal(-Vector2.UnitX, second);
            Assert.Check(movement.QueueDirection(second));
            Assert.Check(!input.TryReadDirection(out _));
            movement.Update(TimeSpan.FromSeconds(1));
            Assert.Equal(-Vector2.UnitX, movement.Cars[0].Direction);
            Assert.Near(440, movement.Cars[0].Center.Y);

            // Reverse event order must override fixed direction priority.
            input.KeyDown(keys[1]);
            input.KeyDown(keys[0]);
            Assert.Check(input.TryReadDirection(out first));
            Assert.Equal(-Vector2.UnitX, first);
            Assert.Check(input.TryReadDirection(out second));
            Assert.Equal(Vector2.UnitY, second);
        }
    }

    private static void KeyboardReversed()
    {
        var input = new KeyboardSteering();
        var effects = new PowerupEffects();
        effects.Collect(PickupType.Mushroom);
        input.KeyDown(Keys.Down);
        input.KeyDown(Keys.Right);
        var movement = new ConvoyMovement(new Vector2(360));
        while (input.TryReadDirection(out Vector2 direction))
        {
            Assert.Check(movement.QueueDirection(effects.MapDirection(direction)));
        }
        movement.Update(effects.Update(TimeSpan.FromSeconds(1)));
        Assert.Equal(-Vector2.UnitX, movement.Cars[0].Direction);
        Assert.Near(280, movement.Cars[0].Center.Y);
    }

    private static void KeyboardRepeat()
    {
        var input = new KeyboardSteering();
        input.KeyDown(Keys.Down);
        Assert.Check(input.TryReadDirection(out _));
        input.KeyDown(Keys.Down);
        input.KeyDown(Keys.Down);
        input.KeyDown(Keys.Space);
        Assert.Check(!input.TryReadDirection(out _));
        input.KeyUp(Keys.Down);
        input.KeyDown(Keys.Down);
        Assert.Check(input.TryReadDirection(out Vector2 direction));
        Assert.Equal(Vector2.UnitY, direction);
        Assert.Check(!input.TryReadDirection(out _));
    }

    private static void KeyboardReset()
    {
        var input = new KeyboardSteering();
        input.KeyDown(Keys.Up);
        input.Reset(new KeyboardState(Keys.Up));
        Assert.Check(!input.TryReadDirection(out _));
        input.KeyDown(Keys.Up);
        Assert.Check(!input.TryReadDirection(out _));
        // KeyUp may have happened outside the window while it was inactive.
        input.Reset(new KeyboardState());
        input.KeyDown(Keys.Up);
        Assert.Check(input.TryReadDirection(out Vector2 direction));
        Assert.Equal(-Vector2.UnitY, direction);
    }
}
