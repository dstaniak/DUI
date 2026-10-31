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
        ("Turning towards a wall still stops at actual contact", TurnsIntoWalls)
    ];

    private static void TurnsClearWalls()
    {
        var cases = new (Vector2 Start, Vector2 Approach, Vector2 Turn)[]
        {
            (new(s_bounds.Left + 75, 500), -Vector2.UnitX, -Vector2.UnitY),
            (new(s_bounds.Right - 75, 500), Vector2.UnitX, -Vector2.UnitY),
            (new(500, s_bounds.Top + 75), -Vector2.UnitY, Vector2.UnitX),
            (new(200, s_bounds.Bottom - 75), Vector2.UnitY, Vector2.UnitX)
        };
        foreach (var scenario in cases)
        {
            ConvoyMovement movement = CreateApproach(scenario.Start, scenario.Approach);
            for (int i = 0; i < 3; i++)
            {
                movement.Grow();
            }
            movement.Stepped += (_, _) =>
            {
                if (s_parking.IntersectsBoundary(movement.Cars[0]))
                {
                    movement.Stop();
                }
            };
            Assert.Check(movement.QueueDirection(scenario.Turn));
            movement.Update(ConvoyMovement.TurnDuration);
            if (movement.HasStopped)
            {
                throw new InvalidOperationException($"Safe turn stopped at {movement.Cars[0]}");
            }
            Assert.Equal(scenario.Turn, movement.Cars[0].Direction);
            // Continue along the wall, including after every follower takes the corner.
            movement.Update(TimeSpan.FromMilliseconds(600));
            Assert.Check(!movement.HasStopped);
        }
    }

    private static ConvoyMovement CreateApproach(Vector2 center, Vector2 direction)
    {
        var probe = new ConvoyMovement(Vector2.Zero);
        FaceDirection(probe, direction);
        var movement = new ConvoyMovement(center - probe.Cars[0].Center);
        FaceDirection(movement, direction);
        Assert.Equal(center, movement.Cars[0].Center);
        return movement;
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

    private static void TurnsIntoWalls()
    {
        var cases = new (Vector2 Start, Vector2 Approach, Vector2 Turn)[]
        {
            (new(s_bounds.Left + 19, 400), -Vector2.UnitY, -Vector2.UnitX),
            (new(s_bounds.Right - 19, 400), -Vector2.UnitY, Vector2.UnitX),
            (new(500, s_bounds.Top + 19), Vector2.UnitX, -Vector2.UnitY),
            (new(500, s_bounds.Bottom - 19), Vector2.UnitX, Vector2.UnitY)
        };
        foreach (var scenario in cases)
        {
            ConvoyMovement movement = CreateApproach(scenario.Start, scenario.Approach);
            Assert.Check(!s_parking.IntersectsBoundary(movement.Cars[0]));
            movement.Stepped += (_, _) =>
            {
                if (s_parking.IntersectsBoundary(movement.Cars[0]))
                {
                    movement.Stop();
                }
            };
            TimeSpan startTime = movement.ElapsedTime;
            Assert.Check(movement.QueueDirection(scenario.Turn));
            movement.Update(ConvoyMovement.TurnDuration);
            Assert.Check(movement.HasStopped);
            Assert.Check(movement.ElapsedTime - startTime < ConvoyMovement.TurnDuration);
            Assert.Check(s_parking.IntersectsBoundary(movement.Cars[0]));
        }
    }

    private static void FaceDirection(ConvoyMovement movement, Vector2 direction)
    {
        if (direction == -Vector2.UnitX)
        {
            movement.QueueDirection(Vector2.UnitY);
            movement.Update(ConvoyMovement.TurnDuration);
        }
        if (direction != Vector2.UnitX)
        {
            movement.QueueDirection(direction);
            movement.Update(ConvoyMovement.TurnDuration);
        }
        // Give the initial followers a straight approach, independent of the setup turns.
        movement.Update(TimeSpan.FromSeconds(1));
    }
}
