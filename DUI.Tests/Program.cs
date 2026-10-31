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
    ("Car sway moves sideways and steers without changing the grid path", SwayIsVisual),
    ("Cars repeat the head's sway with one step of delay per segment", SwayFollowsHead),
    ("Visual car headings and offsets stay continuous at corners", SwayCorners),
    ("Sway freezes on pause, resets on retry and ignores frame size", SwayTiming),
    ("Growing at a corner produces a finite car pose", SwayGrowth),
    ("Entering an island from any side gives one eased hop", HopEntry),
    ("Grass travel, exits and stationary cars do not hop", HopOnlyOnEntry),
    ("Each car hops when its own grid step reaches the island", HopFollowsHead),
    ("Hops freeze on pause, reset on retry and ignore frame size", HopTiming),
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

static void SwayIsVisual()
{
    var movement = CreateMovement();
    var initialPose = CarSway.GetPose(movement, 0);
    Equal(Vector2.Zero, initialPose.Position);
    Check(initialPose.Rotation > 0.1f && initialPose.Rotation < 0.11f);

    movement.Update(TimeSpan.FromMilliseconds(300));
    Equal(new Vector2(120, 0), movement.GetHeadPosition());
    var rightPose = CarSway.GetPose(movement, 0);
    Equal(new Vector2(120, 5), rightPose.Position);
    Check(MathF.Abs(rightPose.Rotation) < 0.0001f);

    movement.Update(TimeSpan.FromMilliseconds(600));
    Equal(new Vector2(360, -5), CarSway.GetPose(movement, 0).Position);
}

static void SwayFollowsHead()
{
    var movement = CreateMovement();
    for (int i = 0; i < 4; i++)
    {
        movement.Grow();
        movement.Update(SnakeMovement.StepDuration);
    }

    var history = new (Vector2 Position, float Rotation)[401];
    history[0] = CarSway.GetPose(movement, 0);
    for (int frame = 1; frame < history.Length; frame++)
    {
        Vector2 turn = frame switch
        {
            55 => -Vector2.UnitY,
            135 => -Vector2.UnitX,
            215 => Vector2.UnitY,
            335 => Vector2.UnitX,
            _ => Vector2.Zero
        };
        if (turn != Vector2.Zero)
        {
            Check(movement.QueueDirection(turn));
        }

        movement.Update(TimeSpan.FromMilliseconds(10));
        history[frame] = CarSway.GetPose(movement, 0);
        for (int i = 1; i < movement.Segments.Count; i++)
        {
            int previousFrame = frame - i * 20;
            if (previousFrame >= 0)
            {
                EqualPose(history[previousFrame], CarSway.GetPose(movement, i));
            }
        }
    }
}

static void SwayCorners()
{
    var movement = CreateMovement();
    foreach (Vector2 turn in new[] { -Vector2.UnitY, -Vector2.UnitX, Vector2.UnitY, Vector2.UnitX })
    {
        Check(movement.QueueDirection(turn));
        movement.Update(SnakeMovement.StepDuration - TimeSpan.FromTicks(1));
        var before = CarSway.GetPose(movement, 0);
        movement.Update(TimeSpan.FromTicks(1));
        EqualPose(before, CarSway.GetPose(movement, 0));
    }
}

static void SwayTiming()
{
    var smallFrames = CreateMovement();
    var largeFrame = CreateMovement();
    for (int i = 0; i < 137; i++)
    {
        smallFrames.Update(TimeSpan.FromMilliseconds(10));
    }
    largeFrame.Update(TimeSpan.FromMilliseconds(1370));
    EqualPose(CarSway.GetPose(smallFrames, 0), CarSway.GetPose(largeFrame, 0));

    var pausedPose = CarSway.GetPose(smallFrames, 0);
    smallFrames.Update(TimeSpan.Zero);
    EqualPose(pausedPose, CarSway.GetPose(smallFrames, 0));

    var restarted = CreateMovement();
    Check(restarted.ElapsedTime == TimeSpan.Zero);
    EqualPose(CarSway.GetPose(CreateMovement(), 0), CarSway.GetPose(restarted, 0));
}

static void SwayGrowth()
{
    var movement = CreateMovement();
    Check(movement.QueueDirection(-Vector2.UnitY));
    movement.Update(TimeSpan.FromMilliseconds(300));
    for (int i = 0; i < 3; i++)
    {
        movement.Grow();
        var pose = CarSway.GetPose(movement, movement.Segments.Count - 1);
        Check(float.IsFinite(pose.Position.X) && float.IsFinite(pose.Position.Y));
        Check(float.IsFinite(pose.Rotation));
    }
}

static void EqualPose((Vector2 Position, float Rotation) expected, (Vector2 Position, float Rotation) actual)
{
    Equal(expected.Position, actual.Position);
    float difference = MathHelper.WrapAngle(expected.Rotation - actual.Rotation);
    Check(float.IsFinite(difference) && MathF.Abs(difference) < 0.0001f);
}

static Rectangle[] HopIslands() => [new Rectangle(160, 240, 160, 320)];

static void HopEntry()
{
    Rectangle[] islands = HopIslands();
    var entries = new (Vector2 From, Vector2 To)[]
    {
        (new(120, 280), new(200, 280)),
        (new(360, 280), new(280, 280)),
        (new(200, 200), new(200, 280)),
        (new(200, 600), new(200, 520))
    };

    foreach (var entry in entries)
    {
        EqualHop((1f, 0f), CarHop.GetTransform(entry.From, entry.To, 0f, islands));
        EqualHop((1.28f, 12f), CarHop.GetTransform(entry.From, entry.To, 0.5f, islands));
        EqualHop((1f, 0f), CarHop.GetTransform(entry.From, entry.To, 1f, islands));

        // Easing lifts the car quickly and slows around the apex.
        Check(CarHop.GetTransform(entry.From, entry.To, 0.25f, islands).Lift > 6f);
        Check(CarHop.GetTransform(entry.From, entry.To, 0.75f, islands).Lift > 6f);
        float previousLift = 0f;
        for (int sample = 0; sample <= 100; sample++)
        {
            var hop = CarHop.GetTransform(entry.From, entry.To, sample / 100f, islands);
            Check(hop.Scale >= 1f && hop.Scale <= 1.28f);
            Check(hop.Lift >= 0f && hop.Lift <= 12f);
            Check(sample <= 50 ? hop.Lift >= previousLift : hop.Lift <= previousLift);
            previousLift = hop.Lift;
        }
    }
}

static void HopOnlyOnEntry()
{
    Rectangle[] islands = HopIslands();
    var paths = new (Vector2 From, Vector2 To)[]
    {
        (new(200, 280), new(280, 280)),
        (new(200, 280), new(200, 360)),
        (new(200, 280), new(120, 280)),
        (new(280, 280), new(360, 280)),
        (new(200, 280), new(200, 200)),
        (new(200, 520), new(200, 600)),
        (new(120, 280), new(120, 360)),
        (new(200, 280), new(200, 280)),
        (new(120, 280), new(120, 280))
    };

    foreach (var path in paths)
    {
        foreach (float progress in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            EqualHop((1f, 0f), CarHop.GetTransform(path.From, path.To, progress, islands));
        }
    }
}

static void HopFollowsHead()
{
    var movement = new SnakeMovement(new Vector2(80, 240), 80);
    for (int i = 0; i < 3; i++)
    {
        movement.Grow();
    }

    var history = new (float Scale, float Lift)[201];
    history[0] = GetHop(movement, 0);
    for (int frame = 1; frame < history.Length; frame++)
    {
        movement.Update(TimeSpan.FromMilliseconds(5));
        history[frame] = GetHop(movement, 0);
        for (int i = 1; i < movement.Segments.Count; i++)
        {
            int previousFrame = frame - i * 40;
            if (previousFrame >= 0)
            {
                EqualHop(history[previousFrame], GetHop(movement, i));
            }
        }
    }

    Check(history[20].Lift == 12f);
    EqualHop((1f, 0f), history[60]);
}

static void HopTiming()
{
    var smallFrames = new SnakeMovement(new Vector2(80, 240), 80);
    var largeFrame = new SnakeMovement(new Vector2(80, 240), 80);
    for (int i = 0; i < 30; i++)
    {
        smallFrames.Update(TimeSpan.FromMilliseconds(5));
    }
    largeFrame.Update(TimeSpan.FromMilliseconds(150));
    EqualHop(GetHop(smallFrames, 0), GetHop(largeFrame, 0));
    Check(GetHop(smallFrames, 0).Lift > 0f);

    var paused = GetHop(smallFrames, 0);
    smallFrames.Update(TimeSpan.Zero);
    EqualHop(paused, GetHop(smallFrames, 0));

    var restarted = new SnakeMovement(new Vector2(80, 240), 80);
    EqualHop((1f, 0f), GetHop(restarted, 0));
    largeFrame.Update(TimeSpan.FromMilliseconds(1000));
    EqualHop((1f, 0f), GetHop(largeFrame, 0));
}

static (float Scale, float Lift) GetHop(SnakeMovement movement, int index)
{
    SnakeSegment segment = movement.Segments[index];
    Vector2 centerOffset = new(40f);
    return CarHop.GetTransform(segment.At + centerOffset, segment.To + centerOffset,
        movement.MovementProgress, HopIslands());
}

static void EqualHop((float Scale, float Lift) expected, (float Scale, float Lift) actual)
{
    Check(float.IsFinite(actual.Scale) && float.IsFinite(actual.Lift));
    Check(MathF.Abs(expected.Scale - actual.Scale) < 0.0001f);
    Check(MathF.Abs(expected.Lift - actual.Lift) < 0.0001f);
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
