using System;
using DUI.GameObjects;
using Microsoft.Xna.Framework;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class WallCollisionTests
{
    private static readonly ParkingGeometry s_parking = new(16, 9);
    private static readonly Rectangle s_bounds = s_parking.DrivingBounds;

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("A close quarter turn clears each wall without a phantom collision", TurnsClearWalls),
        ("Wall contact follows the car sides and rotated corners", RotatedFootprint),
        ("Late input after the last safe turn still collides with the wall", LateTurnsHitWalls)
    ];

    private static readonly (Vector2 Start, Vector2 Approach, Vector2 Turn, float Edge)[] s_turns =
    [
        (new(680, 360), -Vector2.UnitX, -Vector2.UnitY, 160),
        (new(680, 520), Vector2.UnitX, -Vector2.UnitY, 1120),
        (new(680, 520), -Vector2.UnitY, Vector2.UnitX, 240),
        (new(680, 360), Vector2.UnitY, -Vector2.UnitX, 560)
    ];

    private static void TurnsClearWalls()
    {
        foreach (var scenario in s_turns)
        {
            ConvoyMovement movement = CreateApproach(scenario.Start, scenario.Approach,
                scenario.Edge, late: false);
            for (int i = 0; i < 3; i++)
            {
                movement.Grow();
            }
            StopAtBoundary(movement);
            Assert.Check(movement.QueueDirection(scenario.Turn));
            AdvanceUntilFacing(movement, scenario.Turn);
            // All followers must also have time to complete the corner.
            movement.Update(TimeSpan.FromMilliseconds(600));
            Assert.Check(!movement.HasStopped);
        }
    }

    private static ConvoyMovement CreateApproach(Vector2 center, Vector2 direction, float edge, bool late)
    {
        var movement = new ConvoyMovement(center);
        if (direction == -Vector2.UnitX)
        {
            Assert.Check(movement.QueueDirection(Vector2.UnitY));
            AdvanceUntilFacing(movement, Vector2.UnitY);
            // Start this setup turn beyond the grace window, retaining the
            // approach lane and room for all followers to clear the final turn.
            movement.Update(TimeSpan.FromMilliseconds(55));
        }
        if (direction != Vector2.UnitX)
        {
            Assert.Check(movement.QueueDirection(direction));
            AdvanceUntilFacing(movement, direction);
        }

        float target = Vector2.Dot(new Vector2(edge), direction) + (late ? ConvoyMovement.TurnGraceDistance + 4 : -4);
        int steps = 0;
        while (Vector2.Dot(movement.Cars[0].Center, direction) < target)
        {
            movement.Update(ConvoyMovement.StepDuration);
            Assert.Check(++steps < 1000);
        }
        Assert.Check(!s_parking.IntersectsBoundary(movement.Cars[0]));
        return movement;
    }

    private static void StopAtBoundary(ConvoyMovement movement)
    {
        movement.Stepped += (_, _) =>
        {
            if (s_parking.IntersectsBoundary(movement.Cars[0]))
            {
                movement.Stop();
            }
        };
    }

    private static void AdvanceUntilFacing(ConvoyMovement movement, Vector2 direction)
    {
        int steps = 0;
        while (Vector2.Distance(movement.Cars[0].Direction, direction) > 0.0001f)
        {
            movement.Update(ConvoyMovement.StepDuration);
            Assert.Check(++steps < 100 && !movement.HasStopped);
        }
    }

    private static void RotatedFootprint()
    {
        float diagonalExtent = 55f / MathF.Sqrt(2f);
        var orientations = new (float Angle, float Width, float Height)[]
        {
            (0f, 37f, 18f),
            (MathF.PI, 37f, 18f),
            (MathF.PI / 2f, 18f, 37f),
            (-MathF.PI / 2f, 18f, 37f),
            (MathF.PI / 4f, diagonalExtent, diagonalExtent),
            (-MathF.PI / 4f, diagonalExtent, diagonalExtent),
            (3f * MathF.PI / 4f, diagonalExtent, diagonalExtent)
        };
        foreach (var orientation in orientations)
        {
            var contacts = new (Vector2 Center, Vector2 Outward)[]
            {
                (new(s_bounds.Left + orientation.Width, 400), -Vector2.UnitX),
                (new(s_bounds.Right - orientation.Width, 400), Vector2.UnitX),
                (new(500, s_bounds.Top + orientation.Height), -Vector2.UnitY),
                (new(500, s_bounds.Bottom - orientation.Height), Vector2.UnitY)
            };
            foreach (var contact in contacts)
            {
                CarPose inside = new(contact.Center - contact.Outward * 0.01f, orientation.Angle, 0);
                CarPose outside = new(contact.Center + contact.Outward * 0.1f, orientation.Angle, 0);
                Assert.Check(!s_parking.IntersectsBoundary(inside));
                Assert.Check(s_parking.IntersectsBoundary(outside));
            }
        }
    }

    private static void LateTurnsHitWalls()
    {
        foreach (var scenario in s_turns)
        {
            ConvoyMovement movement = CreateApproach(scenario.Start, scenario.Approach,
                scenario.Edge, late: true);
            StopAtBoundary(movement);
            Assert.Check(movement.QueueDirection(scenario.Turn));
            movement.Update(TimeSpan.FromSeconds(1));
            Assert.Check(movement.HasStopped);
            Assert.Check(s_parking.IntersectsBoundary(movement.Cars[0]));
        }
    }
}
