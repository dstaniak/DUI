using System;
using DUI.GameObjects;
using Microsoft.Xna.Framework;
using Assert = DUI.Tests.MovementTests;

namespace DUI.Tests;

internal static class ParkingGeometryTests
{
    private static readonly ParkingGeometry s_parking = new(16, 9);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Visible asphalt inside fence tiles remains driveable", FenceTileAsphalt),
        ("Gate corners use the rotated body, not its bounding box", GateCorners),
        ("The closed gate stops the car before the lower fence rail", GateContact),
        ("Bottle travel stays inside both the fence and the gate", BottleArea)
    ];

    private static void FenceTileAsphalt()
    {
        // All these bodies overlap the formerly blocked outer tile strip,
        // while remaining clear of the actual metal in the parking artwork.
        var safePoses = new CarPose[]
        {
            new(new(85, 400), MathF.PI / 2f, 0),
            new(new(1195, 400), MathF.PI / 2f, 0),
            new(new(400, 625), MathF.PI / 4f, 0),
            new(new(640, 615), MathF.PI / 4f, 0)
        };
        Rectangle oldTileBounds = new(80, 160, 1120, 480);
        foreach (CarPose pose in safePoses)
        {
            Assert.Check(ConvoyCollision.IsOutsideParking(pose, oldTileBounds));
            Assert.Check(!s_parking.IntersectsBoundary(pose));
        }
    }

    private static void GateCorners()
    {
        // The car's bounding box crosses the upper gate corner in both
        // orientations, but only the body pointing into that corner hits it.
        foreach (var scenario in new (float X, float ClearAngle, float HitAngle)[]
        {
            (537, -MathF.PI / 4f, MathF.PI / 4f),
            (743, MathF.PI / 4f, -MathF.PI / 4f)
        })
        {
            CarPose clear = new(new(scenario.X, 627), scenario.ClearAngle, 0);
            CarPose hit = clear with { Rotation = scenario.HitAngle };
            Assert.Check(!s_parking.IntersectsBoundary(clear));
            Assert.Check(!ConvoyCollision.IsOutsideParking(hit, s_parking.DrivingBounds));
            Assert.Check(s_parking.IntersectsBoundary(hit));
            Assert.Check(ConvoyCollision.IntersectsObstacle(hit, s_parking.GateBounds));
        }
    }

    private static void GateContact()
    {
        // The top of the gate is y=654; a downward car reaches it at y=617.
        CarPose beforeGate = new(new(640, 616.9f), MathF.PI / 2f, 0);
        CarPose insideGate = beforeGate with { Center = new(640, 617.1f) };
        Assert.Check(!s_parking.IntersectsBoundary(beforeGate));
        Assert.Check(s_parking.IntersectsBoundary(insideGate));
        Assert.Check(!ConvoyCollision.IsOutsideParking(insideGate, s_parking.DrivingBounds));

        // Away from the gate the same approach can reach the rail at y=666.
        Assert.Check(!s_parking.IntersectsBoundary(insideGate with { Center = new(400, 628.9f) }));
        Assert.Check(s_parking.IntersectsBoundary(insideGate with { Center = new(400, 629.1f) }));
    }

    private static void BottleArea()
    {
        Rectangle area = s_parking.PickupBounds;
        Assert.Check(s_parking.DrivingBounds.Contains(area));
        Assert.Check(!area.Intersects(s_parking.GateBounds));
        Assert.Check(area.Bottom == s_parking.GateBounds.Top);
        Assert.Check(area.Left < 80 && area.Right > 1200 && area.Bottom > 640);
    }
}
