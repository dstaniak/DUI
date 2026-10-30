using System;
using DUI.GameObjects;
using DUI.Input;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

// A GPU-free executable regression suite. Failure returns a nonzero exit code.
var tests = new (string Name, Action Run)[]
{
    ("Initial movement and interpolation match the tutorial", InitialMovement),
    ("Two queued turns execute on consecutive grid steps", QueuedTurns),
    ("Queue capacity is two; overflow does not replace a turn", QueueOverflow),
    ("Reverse is rejected relative to the last queued turn", ReverseIsRejected),
    ("Repeated forward input still consumes a queue slot", RepeatedDirection),
    ("Elapsed time retains the remainder across multiple steps", ElapsedRemainder),
    ("Growth retains the tail and follows the head", Growth),
    ("Turning into the body raises a collision", BodyCollision),
    ("A new game starts with an empty queue and fresh interpolation", NewGame),
    ("Swipe emits once at the threshold and accepts the next contact", Swipe),
    ("Swipe ignores the score bar and margins", SwipeMargins),
    ("Cancelling a swipe cannot steer after resume", CancelSwipe),
    ("Wide and portrait screens preserve geometry and input mapping", DisplayMapping),
    ("Circle contact uses the tutorial's strict intersection rule", CircleCollision)
};

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

static SnakeMovement CreateMovement() => new(Vector2.Zero, 80);

static void InitialMovement()
{
    var movement = CreateMovement();
    Equal(Vector2.Zero, movement.GetHeadPosition());
    movement.Update(TimeSpan.FromMilliseconds(100));
    Equal(new Vector2(40, 0), movement.GetHeadPosition());
    movement.Update(TimeSpan.FromMilliseconds(100));
    Equal(new Vector2(80, 0), movement.GetHeadPosition());
    Equal(new Vector2(160, 0), movement.Segments[0].To);
}

static void QueuedTurns()
{
    var movement = CreateMovement();
    Check(movement.QueueDirection(-Vector2.UnitY));
    Check(movement.QueueDirection(-Vector2.UnitX));
    movement.Update(TimeSpan.FromMilliseconds(199));
    Equal(Vector2.UnitX, movement.Segments[0].Direction);
    movement.Update(TimeSpan.FromMilliseconds(1));
    Equal(-Vector2.UnitY, movement.Segments[0].Direction);
    Equal(new Vector2(80, -80), movement.Segments[0].To);
    movement.Update(TimeSpan.FromMilliseconds(200));
    Equal(-Vector2.UnitX, movement.Segments[0].Direction);
    Equal(new Vector2(0, -80), movement.Segments[0].To);
}

static void QueueOverflow()
{
    var movement = CreateMovement();
    Check(movement.QueueDirection(-Vector2.UnitY));
    Check(movement.QueueDirection(-Vector2.UnitX));
    Check(!movement.QueueDirection(Vector2.UnitY));
    movement.Update(TimeSpan.FromMilliseconds(600));
    Equal(new Vector2(-80, -80), movement.Segments[0].To);
}

static void ReverseIsRejected()
{
    var movement = CreateMovement();
    Check(!movement.QueueDirection(-Vector2.UnitX));
    Check(!movement.QueueDirection(Vector2.Zero));
    Check(!movement.QueueDirection(new Vector2(1, 1)));
    Check(movement.QueueDirection(-Vector2.UnitY));
    Check(!movement.QueueDirection(Vector2.UnitY));
    Check(movement.QueueDirection(-Vector2.UnitX));
}

static void RepeatedDirection()
{
    var movement = CreateMovement();
    Check(movement.QueueDirection(Vector2.UnitX));
    Check(movement.QueueDirection(-Vector2.UnitY));
    movement.Update(TimeSpan.FromMilliseconds(200));
    Equal(Vector2.UnitX, movement.Segments[0].Direction);
    movement.Update(TimeSpan.FromMilliseconds(200));
    Equal(-Vector2.UnitY, movement.Segments[0].Direction);
}

static void ElapsedRemainder()
{
    var movement = CreateMovement();
    movement.Update(TimeSpan.FromMilliseconds(450));
    Equal(new Vector2(180, 0), movement.GetHeadPosition());
    movement.Update(TimeSpan.FromMilliseconds(150));
    Equal(new Vector2(240, 0), movement.GetHeadPosition());
}

static void Growth()
{
    var movement = CreateMovement();
    movement.Grow();
    Check(movement.Segments.Count == 2);
    movement.Update(TimeSpan.FromMilliseconds(200));
    Equal(new Vector2(80, 0), movement.Segments[0].At);
    Equal(Vector2.Zero, movement.Segments[1].At);
    Equal(new Vector2(80, 0), movement.Segments[1].To);
}

static void BodyCollision()
{
    var movement = CreateMovement();
    int collisions = 0;
    movement.BodyCollision += (_, _) => collisions++;
    for (int index = 0; index < 4; index++)
    {
        movement.Grow();
        movement.Update(TimeSpan.FromMilliseconds(200));
    }

    Check(collisions == 0);
    foreach (Vector2 direction in new[] { -Vector2.UnitY, -Vector2.UnitX, Vector2.UnitY, Vector2.UnitX })
    {
        Check(movement.QueueDirection(direction));
        movement.Update(TimeSpan.FromMilliseconds(200));
    }

    Check(collisions == 1);
}

static void NewGame()
{
    var movement = CreateMovement();
    movement.QueueDirection(-Vector2.UnitY);
    movement.Update(TimeSpan.FromMilliseconds(150));
    movement = CreateMovement();
    Equal(Vector2.Zero, movement.GetHeadPosition());
    movement.Update(TimeSpan.FromMilliseconds(200));
    Equal(Vector2.UnitX, movement.Segments[0].Direction);
    Check(movement.Segments.Count == 1);
}

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
        Check(GameDisplay.Width * display.Scale <= size.X);
        Check(GameDisplay.Height * display.Scale <= size.Y);
    }

    display.Resize(0, 0);
    Check(display.Scale > 0);
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
