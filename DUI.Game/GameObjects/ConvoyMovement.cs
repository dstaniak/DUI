using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace DUI.GameObjects;

/// <summary>Continuous steering and distance-spaced followers, independent of rendering and terrain.</summary>
public sealed class ConvoyMovement
{
    public const float Speed = 400f;
    public const float Spacing = 80f;
    public static readonly TimeSpan StepDuration = TimeSpan.FromMilliseconds(5);
    public static readonly TimeSpan TurnDuration = TimeSpan.FromMilliseconds(160);

    private const int MaxQueuedTurns = 2;
    private readonly List<CarPose> _cars = [];
    private readonly Queue<Vector2> _directions = new(MaxQueuedTurns);
    private readonly ConvoyTrail _trail;
    private Vector2 _targetDirection = Vector2.UnitX;
    private float _turnStart;
    private float _turnAngle;
    private long _turnTicks;
    private long _remainingTicks;
    private bool _turning;

    public event EventHandler BodyCollision;

    // Consumers update terrain and bounds after every completed simulation step.
    internal event EventHandler Stepped;

    public IReadOnlyList<CarPose> Cars { get; }
    public TimeSpan ElapsedTime { get; private set; }
    public bool HasStopped { get; private set; }
    public TimeSpan RenderTime => HasStopped ? ElapsedTime :
        TimeSpan.FromTicks(Math.Max(0, ElapsedTime.Ticks - StepDuration.Ticks + _remainingTicks));

    internal int TrailSampleCount => _trail.Count;

    public ConvoyMovement(Vector2 startingCenter)
    {
        CarPose head = new(startingCenter, 0f, 0);
        _cars.Add(head);
        Cars = _cars.AsReadOnly();
        _trail = new ConvoyTrail(head);
    }

    /// <summary>Accepts up to two waiting quarter turns; the active turn always finishes first.</summary>
    public bool QueueDirection(Vector2 direction)
    {
        bool cardinal = direction == Vector2.UnitX || direction == -Vector2.UnitX ||
            direction == Vector2.UnitY || direction == -Vector2.UnitY;
        if (!cardinal || HasStopped)
        {
            return false;
        }

        Vector2 previous = _directions.Count > 0 ? _directions.Last() : _targetDirection;
        if (direction == previous || Vector2.Dot(direction, previous) < 0f ||
            _directions.Count >= MaxQueuedTurns)
        {
            return false;
        }

        _directions.Enqueue(direction);
        return true;
    }

    public void Grow()
    {
        _cars.Add(_trail.Sample(_cars[0].Distance - _cars.Count * Spacing));
    }

    public CarPose GetRenderPose(int index)
    {
        double distance = RenderTime.TotalSeconds * Speed - index * Spacing;
        return _trail.Sample(distance);
    }

    public void Stop()
    {
        HasStopped = true;
        _remainingTicks = 0;
    }

    public void Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        if (HasStopped)
        {
            return;
        }

        _remainingTicks += elapsed.Ticks;
        while (_remainingTicks >= StepDuration.Ticks && !HasStopped)
        {
            _remainingTicks -= StepDuration.Ticks;
            Advance();
        }
    }

    private void Advance()
    {
        BeginQueuedTurn();

        CarPose before = _cars[0];
        float middleHeading = HeadingAt(_turnTicks + StepDuration.Ticks / 2);
        float heading = HeadingAt(_turnTicks + StepDuration.Ticks);
        float distance = Speed * (float)StepDuration.TotalSeconds;
        Vector2 travel = new(MathF.Cos(middleHeading), MathF.Sin(middleHeading));
        CarPose head = new(before.Center + travel * distance, heading, before.Distance + distance);
        _trail.Append(head);
        _cars[0] = head;

        bool collided = MoveFollowers(before, head);
        ElapsedTime += StepDuration;
        FinishTurnStep();
        _trail.TrimBefore(head.Distance - _cars.Count * Spacing - distance);

        Stepped?.Invoke(this, EventArgs.Empty);
        if (collided && !HasStopped)
        {
            Stop();
            BodyCollision?.Invoke(this, EventArgs.Empty);
        }
    }

    private void BeginQueuedTurn()
    {
        if (_turning || _directions.Count == 0)
        {
            return;
        }

        _targetDirection = _directions.Dequeue();
        _turnStart = _cars[0].Rotation;
        float targetAngle = MathF.Atan2(_targetDirection.Y, _targetDirection.X);
        _turnAngle = MathHelper.WrapAngle(targetAngle - _turnStart);
        _turnTicks = 0;
        _turning = true;
    }

    private float HeadingAt(long turnTicks)
    {
        if (!_turning)
        {
            return _cars[0].Rotation;
        }

        float progress = Math.Min(1f, (float)turnTicks / TurnDuration.Ticks);
        return _turnStart + _turnAngle * Easings.EaseInOutSine(progress);
    }

    private void FinishTurnStep()
    {
        if (!_turning)
        {
            return;
        }

        _turnTicks += StepDuration.Ticks;
        if (_turnTicks >= TurnDuration.Ticks)
        {
            _turning = false;
        }
    }

    private bool MoveFollowers(CarPose headBefore, CarPose headAfter)
    {
        bool collided = false;
        for (int index = 1; index < _cars.Count; index++)
        {
            CarPose before = _cars[index];
            CarPose after = _trail.Sample(headAfter.Distance - index * Spacing);
            _cars[index] = after;

            // The directly connected car may overlap the head on a tight bend.
            if (index >= 2 && ConvoyCollision.Intersects(
                headBefore.Center, headAfter.Center, before.Center, after.Center))
            {
                collided = true;
            }
        }

        return collided;
    }
}
