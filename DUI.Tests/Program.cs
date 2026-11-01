using System;
using DUI.Tests;
using System.Linq;
using DUI.GameObjects;
using DUI.Input;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

// A GPU-free executable regression suite. Failure returns a nonzero exit code.
var tests = new (string Name, Action Run)[]
{
    ("Swipe emits once at the threshold and accepts the next contact", Swipe),
    ("Swipe ignores the score bar and margins", SwipeMargins),
    ("Cancelling a swipe cannot steer after resume", CancelSwipe),
    ("Wide and portrait screens keep the board visible and map input", DisplayMapping),
    ("Circle contact uses the strict intersection rule", CircleCollision)
}.Concat(MovementTests.Cases)
    .Concat(CarAnimationTests.Cases)
    .Concat(WallCollisionTests.Cases)
    .Concat(ParkingGeometryTests.Cases)
    .Concat(GameDisplayTests.Cases)
    .Concat(PickupTests.Cases)
    .Concat(ScreenShakeTests.Cases)
    .ToArray();

int failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed.");
return failures == 0 ? 0 : 1;

static void Swipe()
{
    var swipe = new SwipeTracker();
    swipe.Begin(new Vector2(400, 300));
    Equal(Vector2.Zero, swipe.Move(new Vector2(400, 269)));
    Equal(-Vector2.UnitY, swipe.Move(new Vector2(400, 268)));
    Equal(Vector2.Zero, swipe.Move(new Vector2(400, 220)));
    swipe.Begin(new Vector2(400, 300));
    Equal(-Vector2.UnitX, swipe.Move(new Vector2(350, 310)));
}

static void SwipeMargins()
{
    var swipe = new SwipeTracker();
    foreach (Vector2 origin in new[] { new Vector2(100, 20), new Vector2(-10, 300), new Vector2(1300, 300) })
    {
        swipe.Begin(origin);
        Equal(Vector2.Zero, swipe.Move(origin + new Vector2(100, 0)));
    }
}

static void CancelSwipe()
{
    var swipe = new SwipeTracker();
    swipe.Begin(new Vector2(400, 300));
    swipe.Reset();
    Equal(Vector2.Zero, swipe.Move(new Vector2(500, 300)));
}

static void DisplayMapping()
{
    var display = new GameDisplay();
    foreach (Point size in new[] { new Point(2560, 1080), new Point(720, 1280), new Point(1280, 720) })
    {
        display.Resize(size.X, size.Y);
        Equal(new Vector2(640, 360), display.ToGamePosition(new Vector2(size.X / 2f, size.Y / 2f)));
        Vector2 gamePosition = new(200, 300);
        Vector2 screenPosition = Vector2.Transform(gamePosition, display.Transform);
        Equal(gamePosition, display.ToGamePosition(screenPosition));
        Check(display.OutputBounds == new Rectangle(0, 0, size.X, size.Y));
    }

    display.Resize(0, 0);
    Check(display.OutputBounds == new Rectangle(0, 0, 1280, 720));
}

static void CircleCollision()
{
    var circle = new Circle(100, 100, 40);
    Check(!circle.Intersects(new Circle(160, 100, 20)));
    Check(circle.Intersects(new Circle(159, 100, 20)));
}

static void Equal(Vector2 expected, Vector2 actual)
{
    if (!float.IsFinite(actual.X) || !float.IsFinite(actual.Y) ||
        Vector2.DistanceSquared(expected, actual) > 0.0001f)
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void Check(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Condition failed.");
    }
}
