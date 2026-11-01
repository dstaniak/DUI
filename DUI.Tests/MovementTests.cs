using System;
using DUI.GameObjects;
using Microsoft.Xna.Framework;

namespace DUI.Tests;

internal static class MovementTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Straight travel and rendered remainder use board pixels", StraightTravel),
        ("Input waits for the next tile edge before turning", AlignedTurn),
        ("All quarter turns follow constant-speed tangent arcs", QuarterTurns),
        ("An active turn finishes before two queued turns execute", QueuedTurns),
        ("Late input waits for the following tile without teleporting", LateTurn),
        ("Repeated turns keep both logical and rendered poses on grid lanes", GridLanes),
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
        Equal(new Vector2(120, 440), movement.GetRenderPose(0).Center);
        movement.Update(TimeSpan.FromMilliseconds(103));
        Equal(new Vector2(160, 440), movement.Cars[0].Center);
        Equal(new Vector2(159.2f, 440), movement.GetRenderPose(0).Center);
        Near(40, movement.Cars[0].Distance);
        movement.Update(TimeSpan.FromMilliseconds(2));
        Equal(new Vector2(162, 440), movement.Cars[0].Center);
        Equal(new Vector2(160, 440), movement.GetRenderPose(0).Center);
    }

    private static void AlignedTurn()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        movement.Update(TimeSpan.FromMilliseconds(73));
        Check(movement.QueueDirection(Vector2.UnitY));
        movement.Update(TimeSpan.FromMilliseconds(27));
        Equal(new Vector2(80, 40), movement.Cars[0].Center);
        Near(0, movement.Cars[0].Rotation);
        movement.Update(TimeSpan.FromMilliseconds(10));
        Check(movement.Cars[0].Center.Y > 40f);
        Check(movement.Cars[0].Rotation > 0f);
        Check(movement.GetRenderPose(0).Rotation > 0f);
        movement.Update(TimeSpan.FromMilliseconds(200));
        Near(120, movement.Cars[0].Center.X);
        Near(120, movement.GetRenderPose(0).Center.X);
    }

    private static void QuarterTurns()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        foreach (Vector2 direction in new[] { Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY, Vector2.UnitX })
        {
            Check(movement.QueueDirection(direction));
            int steps = 0;
            do
            {
                CarPose before = movement.Cars[0];
                movement.Update(ConvoyMovement.StepDuration);
                Near(2, movement.Cars[0].Distance - before.Distance);
                // A 2 px arc chord is slightly shorter than the distance travelled.
                Near(2f, Vector2.Distance(before.Center, movement.Cars[0].Center));
                float angle = MathHelper.WrapAngle(movement.Cars[0].Rotation - before.Rotation);
                Check(angle >= -0.0001f && angle <= 0.1001f);
                Check(++steps < 100);
            } while (Vector2.Distance(direction, movement.Cars[0].Direction) > 0.0001f);
        }

        var turn = new ConvoyMovement(Vector2.Zero);
        turn.QueueDirection(Vector2.UnitY);
        turn.Update(TimeSpan.FromMilliseconds(260));
        // First 40 px reach the tile edge, then a radius-40 quarter circle.
        float remainder = 104f - 40f - MathF.PI * 20f;
        Equal(new Vector2(120, 80 + remainder), turn.Cars[0].Center);
        Near(104, turn.Cars[0].Distance);
    }

    private static void QueuedTurns()
    {
        var movement = new ConvoyMovement(Vector2.Zero);
        Check(movement.QueueDirection(-Vector2.UnitY));
        Check(movement.QueueDirection(-Vector2.UnitX));
        Check(!movement.QueueDirection(Vector2.UnitY));
        movement.Update(TimeSpan.FromMilliseconds(105));
        Check(movement.QueueDirection(Vector2.UnitY));
        Check(!movement.QueueDirection(Vector2.UnitX));
        for (int step = 0; step < 99; step++)
        {
            float previous = movement.Cars[0].Rotation;
            movement.Update(ConvoyMovement.StepDuration);
            float change = MathHelper.WrapAngle(movement.Cars[0].Rotation - previous);
            Check(change <= 0.0001f && change >= -0.0501f);
        }
        Equal(Vector2.UnitY, movement.Cars[0].Direction);
        Near(40, movement.Cars[0].Center.X);
        Near(240 - 40 - 3 * Math.PI * 20, movement.Cars[0].Center.Y);
    }

    private static void LateTurn()
    {
        var movement = new ConvoyMovement(new Vector2(40));
        movement.Update(TimeSpan.FromMilliseconds(155));
        Check(movement.QueueDirection(Vector2.UnitY));
        movement.Update(TimeSpan.FromMilliseconds(145));
        Equal(new Vector2(160, 40), movement.Cars[0].Center);
        Equal(Vector2.UnitX, movement.Cars[0].Direction);
        movement.Update(TimeSpan.FromMilliseconds(200));
        Near(200, movement.Cars[0].Center.X);
        Equal(Vector2.UnitY, movement.Cars[0].Direction);
    }

    private static void GridLanes()
    {
        var movement = new ConvoyMovement(new Vector2(680, 360));
        Vector2[] directions = [Vector2.UnitY, Vector2.UnitX, -Vector2.UnitY, Vector2.UnitX];
        for (int turn = 0; turn < 500; turn++)
        {
            Vector2 direction = directions[turn % directions.Length];
            Check(movement.QueueDirection(direction));
            movement.Update(TimeSpan.FromMilliseconds(400 + turn % 37));
            foreach (CarPose pose in new[] { movement.Cars[0], movement.GetRenderPose(0) })
            {
                Equal(direction, pose.Direction);
                float across = direction.X == 0 ? pose.Center.X : pose.Center.Y;
                Near(Math.Round((across - 40) / 80), (across - 40) / 80);
            }
        }
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
            Equal(new Vector2(40 - 80 * car, 40), movement.Cars[car].Center);
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
        Equal(new Vector2(47960, 40), movement.Cars[1].Center);
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
        Near(120, movement.Cars[0].Center.Y);
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
        Check(movement.QueueDirection(Vector2.UnitY));
        Check(movement.QueueDirection(-Vector2.UnitX));
        movement.Update(TimeSpan.FromMilliseconds(105));
        Check(movement.QueueDirection(-Vector2.UnitY));
        movement.Update(TimeSpan.FromSeconds(1));
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
        var movement = new ConvoyMovement(new Vector2(1160, 280));
        movement.Stepped += (_, _) =>
        {
            if (ConvoyCollision.IsOutsideParking(movement.Cars[0], bounds))
            {
                movement.Stop();
            }
        };
        movement.Update(TimeSpan.FromSeconds(10));
        Check(movement.ElapsedTime == ConvoyMovement.StepDuration * 2);
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
        EqualPose(new CarPose(new Vector2(40), 0, 0), movement.Cars[0]);
        Check(movement.ElapsedTime == TimeSpan.Zero);
        movement.Update(TimeSpan.FromMilliseconds(200));
        Equal(new Vector2(120, 40), movement.Cars[0].Center);
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
