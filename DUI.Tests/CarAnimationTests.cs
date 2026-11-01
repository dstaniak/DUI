using System;
using System.Linq;
using DUI.GameObjects;
using Microsoft.Xna.Framework;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class CarAnimationTests
{
    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Sway stays within the reduced offset and angle limits", SwayLimits),
        ("Curb entry from all four sides gives an eased 200 ms hop", HopEntry),
        ("Diagonal front crossings trigger at the actual curb", DiagonalEntry),
        ("Driving on grass, leaving and standing still do not hop", NoFalseHop),
        ("An active hop cannot restart; leaving re-arms the next entry", HopReentry),
        ("Each follower repeats the head's hop on a curved entry", FollowingHops),
        ("Hop timing survives frame partitioning, pause and restart", HopTiming)
    ];

    private static Rectangle[] Islands() => [new Rectangle(160, 240, 160, 320)];

    private static void SwayLimits()
    {
        for (int sample = 0; sample <= 480; sample++)
        {
            CarPose logical = new(new Vector2(100, 200), MathF.PI / 4, sample);
            CarPose visual = CarSway.GetPose(logical);
            Assert.Check(Vector2.Distance(logical.Center, visual.Center) <= 1.5001f);
            Assert.Check(MathF.Abs(visual.Rotation - logical.Rotation) <= MathF.PI / 90 + 0.0001f);
            Assert.Near(logical.Distance, visual.Distance);
        }
        CarPose peak = CarSway.GetPose(new CarPose(new Vector2(120, 0), 0, 120));
        Assert.Equal(new Vector2(120, 1.5f), peak.Center);
        Assert.Near(0, peak.Rotation);
    }

    private static void HopEntry()
    {
        var entries = new (Vector2 Before, Vector2 After, float Angle)[]
        {
            (new(119, 280), new(121, 280), 0),
            (new(361, 280), new(359, 280), MathF.PI),
            (new(200, 199), new(200, 201), MathF.PI / 2),
            (new(200, 601), new(200, 599), -MathF.PI / 2)
        };
        foreach (var entry in entries)
        {
            var hop = new CarHop();
            hop.Advance(new(entry.Before, entry.Angle, 0), new(entry.After, entry.Angle, 2),
                ConvoyMovement.StepDuration, Islands());
            EqualHop((1, 0), hop.GetTransform(TimeSpan.FromMilliseconds(2.5)));
            EqualHop((1.28f, 12), hop.GetTransform(TimeSpan.FromMilliseconds(102.5)));
            EqualHop((1, 0), hop.GetTransform(TimeSpan.FromMilliseconds(202.5)));
            Assert.Check(hop.GetTransform(TimeSpan.FromMilliseconds(52.5)).Lift > 6);
            Assert.Check(hop.GetTransform(TimeSpan.FromMilliseconds(152.5)).Lift > 6);
            float previous = 0;
            for (int i = 0; i <= 200; i++)
            {
                var value = hop.GetTransform(TimeSpan.FromMilliseconds(2.5 + i));
                Assert.Check(value.Scale >= 1 && value.Scale <= 1.28001f);
                Assert.Check(value.Lift >= 0 && value.Lift <= 12.0001f);
                Assert.Check(i <= 100 ? value.Lift >= previous : value.Lift <= previous);
                previous = value.Lift;
            }
        }
    }

    private static void DiagonalEntry()
    {
        float angle = MathF.PI / 4;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 40;
        var hop = new CarHop();
        hop.Advance(new(new Vector2(159, 260) - offset, angle, 0),
            new(new Vector2(161, 262) - offset, angle, Math.Sqrt(8)),
            ConvoyMovement.StepDuration, Islands());
        EqualHop((1.28f, 12), hop.GetTransform(TimeSpan.FromMilliseconds(102.5)));
    }

    private static void NoFalseHop()
    {
        var paths = new (Vector2 Before, Vector2 After, float Angle)[]
        {
            (new(200, 280), new(202, 280), 0),
            (new(200, 280), new(198, 280), MathF.PI),
            (new(279, 280), new(281, 280), 0),
            (new(200, 519), new(200, 521), MathF.PI / 2),
            (new(120, 280), new(120, 280), 0),
            (new(120, 200), new(122, 200), 0)
        };
        foreach (var path in paths)
        {
            var hop = new CarHop();
            hop.Advance(new(path.Before, path.Angle, 0), new(path.After, path.Angle, 2),
                ConvoyMovement.StepDuration, Islands());
            EqualHop((1, 0), hop.GetTransform(TimeSpan.FromMilliseconds(100)));
        }
    }

    private static void HopReentry()
    {
        var hop = new CarHop();
        CarPose before = new(new Vector2(119, 280), 0, 0);
        CarPose after = new(new Vector2(121, 280), 0, 2);
        hop.Advance(before, after, ConvoyMovement.StepDuration, Islands());
        hop.Advance(after, before, TimeSpan.FromMilliseconds(10), Islands());
        hop.Advance(before, after, TimeSpan.FromMilliseconds(15), Islands());
        EqualHop((1.28f, 12), hop.GetTransform(TimeSpan.FromMilliseconds(102.5)));
        hop.Advance(after, before, TimeSpan.FromMilliseconds(300), Islands());
        hop.Advance(before, after, TimeSpan.FromMilliseconds(305), Islands());
        EqualHop((1.28f, 12), hop.GetTransform(TimeSpan.FromMilliseconds(402.5)));
    }

    private static void FollowingHops()
    {
        var (movement, hops) = CreateHoppingConvoy();
        movement.QueueDirection(Vector2.UnitY);
        var history = new (float Scale, float Lift)[201];
        bool hopped = false;
        for (int step = 0; step < history.Length; step++)
        {
            history[step] = hops[0].GetTransform(movement.RenderTime);
            hopped |= history[step].Lift > 0;
            for (int car = 1; car < hops.Length; car++)
            {
                int earlier = step - car * 40;
                if (earlier >= 0)
                {
                    EqualHop(history[earlier], hops[car].GetTransform(movement.RenderTime));
                }
            }
            movement.Update(ConvoyMovement.StepDuration);
        }
        Assert.Check(hopped);
    }

    private static void HopTiming()
    {
        var (small, smallHops) = CreateHoppingConvoy();
        var (large, largeHops) = CreateHoppingConvoy();
        small.QueueDirection(Vector2.UnitY);
        large.QueueDirection(Vector2.UnitY);
        for (int i = 0; i < 38; i++)
        {
            small.Update(TimeSpan.FromMilliseconds(7));
        }
        large.Update(TimeSpan.FromMilliseconds(266));
        var paused = smallHops[0].GetTransform(small.RenderTime);
        Assert.Check(paused.Lift > 0);
        EqualHop(paused, largeHops[0].GetTransform(large.RenderTime));
        small.Update(TimeSpan.Zero);
        EqualHop(paused, smallHops[0].GetTransform(small.RenderTime));
        var (restart, restartHops) = CreateHoppingConvoy();
        EqualHop((1, 0), restartHops[0].GetTransform(restart.RenderTime));
        small.Stop();
        var stopped = smallHops[0].GetTransform(small.RenderTime);
        small.Update(TimeSpan.FromSeconds(1));
        EqualHop(stopped, smallHops[0].GetTransform(small.RenderTime));
    }

    private static (ConvoyMovement Movement, CarHop[] Hops) CreateHoppingConvoy()
    {
        var movement = new ConvoyMovement(new Vector2(130, 190));
        movement.Grow();
        movement.Grow();
        CarPose[] previous = movement.Cars.ToArray();
        CarHop[] hops = [new(), new(), new()];
        Rectangle[] islands = Islands();
        movement.Stepped += (_, _) =>
        {
            for (int car = 0; car < hops.Length; car++)
            {
                hops[car].Advance(previous[car], movement.Cars[car], movement.ElapsedTime, islands);
                previous[car] = movement.Cars[car];
            }
        };
        return (movement, hops);
    }

    private static void EqualHop((float Scale, float Lift) expected, (float Scale, float Lift) actual)
    {
        Assert.Near(expected.Scale, actual.Scale);
        Assert.Near(expected.Lift, actual.Lift);
    }
}
