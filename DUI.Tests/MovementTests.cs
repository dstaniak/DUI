using System;
using DUI.GameObjects;
using Microsoft.Xna.Framework;

namespace DUI.Tests;

internal static class MovementTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Straight travel and rendered remainder use board pixels", StraightTravel),
        ("Input between tiles starts curving in the next substep", ImmediateTurn),
        ("All quarter turns follow a constant-speed eased trajectory", QuarterTurns),
        ("An active turn finishes before two queued turns execute", QueuedTurns),
        ("Invalid, reverse and repeated targets do not occupy the queue", InvalidTargets),
        ("Fixed simulation and rendering are independent of frame partitions", FramePartitions),
        ("Followers replay the head's positions and headings through four bends", FollowTrail),
        ("Growth samples retained history without moving existing cars", Growth),
        ("Initial growth extends the trail backwards with finite poses", EarlyGrowth),
        ("Trail storage stays bounded during long drives", BoundedTrail),
        ("A tight two-turn reversal does not collide with connected cars", TightReversal),
        ("Crossing the tail stops once and discards remaining simulation time", BodyCollision),
        ("Moving circles detect crossings between separated endpoints", SweptCollision),
        ("Parking boundaries use the car footprint and stop the current update", ParkingBounds),
        ("Pause and restart preserve or clear the complete movement state", PauseAndRestart)
    ];

    private static void StraightTravel()
    {
        var movement = new ConvoyMovement(new Vector2(123, 456));
        Equal(new Vector2(123, 456), movement.GetRenderPose(0).Center);
        movement.Update(TimeSpan.FromMilliseconds(103));
        Equal(new Vector2(163, 456), movement.Cars[0].Center);
        Equal(new Vector2(162.2f, 456), movement.GetRenderPose(0).Center);
        Near(40, movement.Cars[0].Distance);
        movement.Update(TimeSpan.FromMilliseconds(2));
        Equal(new Vector2(165, 456), movement.Cars[0].Center);
        Equal(new Vector2(163, 456), movement.GetRenderPose(0).Center);
    }

    private static void ImmediateTurn()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        movement.Update(TimeSpan.FromMilliseconds(73));
        Check(movement.QueueDirection(Vector2.UnitY));
        movement.Update(TimeSpan.FromMilliseconds(7));
        Check(movement.Cars[0].Center.Y > 0f);
        Check(movement.Cars[0].Rotation > 0f);
        Check(movement.Cars[0].Center.X < 40f);
        Check(movement.GetRenderPose(0).Rotation > 0f);
    }

    private static void QuarterTurns()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        foreach (Vector2 direction in new[] { Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY, Vector2.UnitX })
        {
            float startAngle = movement.Cars[0].Rotation;
            Check(movement.QueueDirection(direction));
            for (int step = 0; step < 32; step++)
            {
                CarPose before = movement.Cars[0];
                movement.Update(ConvoyMovement.StepDuration);
                Near(2f, Vector2.Distance(before.Center, movement.Cars[0].Center));
                Near(2, movement.Cars[0].Distance - before.Distance);
                if (step == 15)
                {
                    Near(0f, MathHelper.WrapAngle(movement.Cars[0].Rotation - startAngle - MathF.PI / 4));
                }
            }
            Equal(direction, movement.Cars[0].Direction);
        }

        var turn = new ConvoyMovement(Vector2.Zero);
        turn.QueueDirection(Vector2.UnitY);
        turn.Update(ConvoyMovement.TurnDuration);
        // Independently integrated quarter turn: both endpoint offsets are about 38.54 px.
        Equal(new Vector2(38.54f), turn.Cars[0].Center, 0.02f);
        Near(64, turn.Cars[0].Distance);
    }

    private static void QueuedTurns()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        Check(movement.QueueDirection(-Vector2.UnitY));
        Check(movement.QueueDirection(-Vector2.UnitX));
        Check(!movement.QueueDirection(Vector2.UnitY));
        movement.Update(ConvoyMovement.StepDuration);
        Check(movement.QueueDirection(Vector2.UnitY));
        Check(!movement.QueueDirection(Vector2.UnitX));
        movement.Update(TimeSpan.FromMilliseconds(155));
        Equal(-Vector2.UnitY, movement.Cars[0].Direction);
        movement.Update(ConvoyMovement.TurnDuration);
        Equal(-Vector2.UnitX, movement.Cars[0].Direction);
        movement.Update(ConvoyMovement.TurnDuration);
        Equal(Vector2.UnitY, movement.Cars[0].Direction);
    }

    private static void InvalidTargets()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        foreach (Vector2 invalid in new[] { Vector2.Zero, Vector2.One, -Vector2.UnitX, Vector2.UnitX })
        {
            Check(!movement.QueueDirection(invalid));
        }
        Check(movement.QueueDirection(Vector2.UnitY));
        Check(!movement.QueueDirection(-Vector2.UnitY));
        Check(!movement.QueueDirection(Vector2.UnitY));
        Check(movement.QueueDirection(-Vector2.UnitX));
    }

    private static void FramePartitions()
    {
        var small = new ConvoyMovement(Vector2.Zero);
        var large = new ConvoyMovement(Vector2.Zero);
        foreach (var movement in new[] { small, large })
        {
            movement.Grow();
            movement.Grow();
            movement.QueueDirection(Vector2.UnitY);
            movement.QueueDirection(-Vector2.UnitX);
        }
        for (int i = 0; i < 137; i++)
        {
            small.Update(TimeSpan.FromMilliseconds(7));
        }
        large.Update(TimeSpan.FromMilliseconds(959));
        Check(small.ElapsedTime == large.ElapsedTime);
        Check(small.RenderTime == large.RenderTime);
        for (int i = 0; i < small.Cars.Count; i++)
        {
            EqualPose(small.Cars[i], large.Cars[i]);
            EqualPose(small.GetRenderPose(i), large.GetRenderPose(i));
        }
    }

    private static void FollowTrail()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        for (int i = 0; i < 3; i++)
        {
            movement.Grow();
        }
        var history = new CarPose[701];
        var rendered = new CarPose[701];
        history[0] = movement.Cars[0];
        rendered[0] = movement.GetRenderPose(0);
        for (int step = 1; step < history.Length; step++)
        {
            Vector2 turn = step switch
            {
                100 => Vector2.UnitY,
                250 => -Vector2.UnitX,
                400 => -Vector2.UnitY,
                550 => Vector2.UnitX,
                _ => Vector2.Zero
            };
            movement.QueueDirection(turn);
            movement.Update(ConvoyMovement.StepDuration);
            Check(!movement.HasStopped);
            history[step] = movement.Cars[0];
            rendered[step] = movement.GetRenderPose(0);
            for (int car = 1; car < movement.Cars.Count; car++)
            {
                Near(car * 80, history[step].Distance - movement.Cars[car].Distance);
                int earlier = step - car * 40;
                if (earlier > 0)
                {
                    EqualPose(history[earlier], movement.Cars[car]);
                    EqualPose(rendered[earlier], movement.GetRenderPose(car));
                    EqualPose(CarSway.GetPose(rendered[earlier]), CarSway.GetPose(movement.GetRenderPose(car)));
                }
            }
        }
    }

    private static void Growth()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        movement.Update(TimeSpan.FromMilliseconds(100));
        CarPose expectedTail = movement.Cars[0];
        movement.QueueDirection(Vector2.UnitY);
        movement.Update(TimeSpan.FromMilliseconds(200));
        CarPose head = movement.Cars[0];
        CarPose renderedHead = movement.GetRenderPose(0);
        movement.Grow();
        EqualPose(expectedTail, movement.Cars[1]);
        EqualPose(head, movement.Cars[0]);
        EqualPose(renderedHead, movement.GetRenderPose(0));
    }

    private static void EarlyGrowth()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        for (int car = 1; car <= 5; car++)
        {
            movement.Grow();
            Equal(new Vector2(-80 * car, 0), movement.Cars[car].Center);
            EqualPose(movement.Cars[car], movement.GetRenderPose(car));
        }
        movement.QueueDirection(Vector2.UnitY);
        movement.Update(TimeSpan.FromMilliseconds(80));
        movement.Grow();
        Check(float.IsFinite(movement.Cars[^1].Rotation));
        Check(float.IsFinite(movement.Cars[^1].Center.X));
    }

    private static void BoundedTrail()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        movement.Update(TimeSpan.FromMinutes(2));
        Check(movement.TrailSampleCount <= 44);
        movement.Grow();
        Equal(new Vector2(47920, 0), movement.Cars[1].Center);
    }

    private static void TightReversal()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        for (int i = 0; i < 5; i++)
        {
            movement.Grow();
        }
        movement.QueueDirection(Vector2.UnitY);
        movement.QueueDirection(-Vector2.UnitX);
        movement.Update(TimeSpan.FromSeconds(1));
        Check(!movement.HasStopped);
        Near(77.08, movement.Cars[0].Center.Y, 0.03);
    }

    private static void BodyCollision()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        for (int i = 0; i < 4; i++)
        {
            movement.Grow();
        }
        int collisions = 0;
        movement.BodyCollision += (_, _) => collisions++;
        foreach (Vector2 turn in new[] { Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY })
        {
            Check(movement.QueueDirection(turn));
            movement.Update(ConvoyMovement.TurnDuration);
        }
        Check(collisions == 1 && movement.HasStopped);
        CarPose stopped = movement.Cars[0];
        movement.Update(TimeSpan.FromSeconds(10));
        Check(collisions == 1);
        EqualPose(stopped, movement.Cars[0]);
        EqualPose(stopped, movement.GetRenderPose(0));
        Check(!movement.QueueDirection(Vector2.UnitX));
    }

    private static void SweptCollision()
    {
        Check(ConvoyCollision.Intersects(new(-100, 0), new(100, 0), Vector2.Zero, Vector2.Zero));
        Check(!ConvoyCollision.Intersects(new(-100, 64), new(100, 64), Vector2.Zero, Vector2.Zero));
        Check(!ConvoyCollision.Intersects(new(0, 0), new(100, 0), new(80, 0), new(180, 0)));
        Check(ConvoyCollision.Intersects(new(0, 0), new(100, 0), new(150, 0), new(50, 0)));
    }

    private static void ParkingBounds()
    {
        Rectangle bounds = new(80, 160, 1120, 480);
        foreach (Vector2 center in new[] { new Vector2(117, 178), new Vector2(1163, 622) })
        {
            Check(!ConvoyCollision.IsOutsideParking(new CarPose(center, 0f, 0), bounds));
        }
        foreach (Vector2 center in new[] { new Vector2(116.9f, 300), new Vector2(1163.1f, 300),
            new Vector2(500, 177.9f), new Vector2(500, 622.1f) })
        {
            Check(ConvoyCollision.IsOutsideParking(new CarPose(center, 0f, 0), bounds));
        }
        var movement = new ConvoyMovement(new Vector2(1162, 300));
        movement.Stepped += (_, _) =>
        {
            if (ConvoyCollision.IsOutsideParking(movement.Cars[0], bounds))
            {
                movement.Stop();
            }
        };
        movement.Update(TimeSpan.FromSeconds(10));
        Check(movement.ElapsedTime == ConvoyMovement.StepDuration);
    }

    private static void PauseAndRestart()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        movement.QueueDirection(Vector2.UnitY);
        movement.QueueDirection(-Vector2.UnitX);
        movement.Update(TimeSpan.FromMilliseconds(83));
        CarPose paused = movement.GetRenderPose(0);
        movement.Update(TimeSpan.Zero);
        EqualPose(paused, movement.GetRenderPose(0));
        movement = new ConvoyMovement(Vector2.Zero);
        EqualPose(new CarPose(Vector2.Zero, 0, 0), movement.Cars[0]);
        Check(movement.ElapsedTime == TimeSpan.Zero);
        movement.Update(TimeSpan.FromMilliseconds(200));
        Equal(new Vector2(80, 0), movement.Cars[0].Center);
    }

    internal static void EqualPose(CarPose expected, CarPose actual)
    {
        Equal(expected.Center, actual.Center);
        Near(0, MathHelper.WrapAngle(expected.Rotation - actual.Rotation));
        Near(expected.Distance, actual.Distance);
    }

    internal static void Equal(Vector2 expected, Vector2 actual, float tolerance = 0.001f)
    {
        if (!float.IsFinite(actual.X) || !float.IsFinite(actual.Y) || Vector2.Distance(expected, actual) > tolerance)
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}");
        }
    }

    internal static void Near(double expected, double actual, double tolerance = 0.001)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}");
        }
    }

    internal static void Check(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Assertion failed");
        }
    }
}
