using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Tile-centered lanes joined by smooth arcs, with distance-spaced followers.</summary>
public sealed class ConvoyMovement
{
    public const float Speed = 400f;
    public const float Spacing = ParkingGeometry.TileSize;
    public static readonly TimeSpan StepDuration = TimeSpan.FromMilliseconds(5);
    public static readonly TimeSpan TurnDuration = TimeSpan.FromSeconds(TurnLength / Speed);

    private const float TurnRadius = ParkingGeometry.TileSize * 0.5f;
    private const double TurnLength = Math.PI * TurnRadius * 0.5;
    private const int MaxQueuedTurns = 2;
    private readonly List<CarPose> _cars = [];
    private readonly Queue<Vector2> _directions = new(MaxQueuedTurns);
    private readonly ConvoyTrail _trail;
    private Vector2 _direction = Vector2.UnitX;
    private Vector2 _targetDirection = Vector2.UnitX;
    private Vector2 _turnEntry;
    private double _turnDistance;
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
        // All straight lanes pass through tile centers, including after restart.
        startingCenter = new Vector2(
            MathF.Floor(startingCenter.X / Spacing) * Spacing + TurnRadius,
            MathF.Floor(startingCenter.Y / Spacing) * Spacing + TurnRadius);
        CarPose head = new(startingCenter, 0f, 0);
        _cars.Add(head);
        Cars = _cars.AsReadOnly();
        _trail = new ConvoyTrail(head);
    }

    /// <summary>Queues up to two quarter turns, starting at the next tile edge on the current lane.</summary>
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
        CarPose before = _cars[0];
        float distance = Speed * (float)StepDuration.TotalSeconds;
        CarPose head = AdvanceHead(before, distance);
        _cars[0] = head;

        bool collided = MoveFollowers(before, head);
        ElapsedTime += StepDuration;
        _trail.TrimBefore(head.Distance - _cars.Count * Spacing - distance);

        Stepped?.Invoke(this, EventArgs.Empty);
        if (collided && !HasStopped)
        {
            Stop();
            BodyCollision?.Invoke(this, EventArgs.Empty);
        }
    }

    private CarPose AdvanceHead(CarPose head, double remainingDistance)
    {
        // Split at lane/arc boundaries even when they fall inside a 5 ms step.
        // Recording those joins keeps render interpolation and followers on the same path.
        while (remainingDistance > 0)
        {
            if (_turning)
            {
                double distance = Math.Min(remainingDistance, TurnLength - _turnDistance);
                head = AdvanceTurn(head, distance);
                remainingDistance -= distance;
            }
            else
            {
                double distance = remainingDistance;
                if (_directions.Count > 0)
                {
                    double coordinate = Vector2.Dot(head.Center, _direction);
                    double nextEdge = Math.Ceiling(coordinate / Spacing) * Spacing;
                    distance = Math.Min(distance, nextEdge - coordinate);
                    if (distance == 0)
                    {
                        _targetDirection = _directions.Dequeue();
                        _turnEntry = head.Center;
                        _turnDistance = 0;
                        _turning = true;
                        continue;
                    }
                }

                head = new CarPose(head.Center + _direction * (float)distance,
                    head.Rotation, head.Distance + distance);
                remainingDistance -= distance;
            }

            _trail.Append(head);
        }

        return head;
    }

    private CarPose AdvanceTurn(CarPose head, double distance)
    {
        _turnDistance += distance;
        Vector2 center;
        Vector2 tangent;
        if (_turnDistance >= TurnLength)
        {
            // Use the exact endpoint instead of accumulated trigonometry.
            // A pair of turns therefore moves to the next lane exactly 80 px away.
            center = _turnEntry + (_direction + _targetDirection) * TurnRadius;
            tangent = _targetDirection;
            _direction = _targetDirection;
            _turning = false;
        }
        else
        {
            double angle = _turnDistance / TurnRadius;
            float sine = (float)Math.Sin(angle);
            float cosine = (float)Math.Cos(angle);
            center = _turnEntry + _direction * (TurnRadius * sine) +
                _targetDirection * (TurnRadius * (1f - cosine));
            tangent = _direction * cosine + _targetDirection * sine;
        }

        return new CarPose(center, MathF.Atan2(tangent.Y, tangent.X), head.Distance + distance);
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
