using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace DUI.GameObjects;

/// <summary>Owns the tutorial movement queue, grid segments and interpolation.</summary>
public sealed class SnakeMovement
{
    private const int MaxBufferSize = 2;
    private static readonly TimeSpan s_movementTime = TimeSpan.FromMilliseconds(200);

    private TimeSpan _movementTimer;
    private float _movementProgress;
    private Vector2 _nextDirection;
    private readonly float _stride;
    private readonly List<SnakeSegment> _segments;
    private readonly Queue<Vector2> _inputBuffer;

    public SnakeMovement(Vector2 startingPosition, float stride)
    {
        _segments = [];
        Segments = _segments.AsReadOnly();
        _stride = stride;

        SnakeSegment head = new();
        head.At = startingPosition;
        head.To = startingPosition + new Vector2(_stride, 0.0f);
        head.Direction = Vector2.UnitX;
        head.PreviousDirection = head.Direction;

        _segments.Add(head);

        _nextDirection = head.Direction;
        _movementTimer = TimeSpan.Zero;

        _inputBuffer = new(MaxBufferSize);
    }

    public event EventHandler BodyCollision;

    public IReadOnlyList<SnakeSegment> Segments { get; }
    public float MovementProgress => _movementProgress;
    public TimeSpan ElapsedTime { get; private set; }
    public static TimeSpan StepDuration => s_movementTime;

    /// <summary>
    /// Queues at most two future directions. Each grid step consumes one.
    /// Validate against the last queued turn, so right, up, left is accepted.
    /// </summary>
    public bool QueueDirection(Vector2 direction)
    {
        bool isCardinal = direction == Vector2.UnitX || direction == -Vector2.UnitX ||
            direction == Vector2.UnitY || direction == -Vector2.UnitY;
        if (!isCardinal || _inputBuffer.Count >= MaxBufferSize)
        {
            return false;
        }

        Vector2 previousDirection = _inputBuffer.Count > 0
            ? _inputBuffer.Last()
            : _segments[0].Direction;
        if (Vector2.Dot(direction, previousDirection) < 0)
        {
            return false;
        }

        _inputBuffer.Enqueue(direction);
        return true;
    }

    public void Grow()
    {
        SnakeSegment tail = _segments[_segments.Count - 1];
        SnakeSegment newTail = new();
        newTail.At = tail.To + tail.ReverseDirection * _stride;
        newTail.To = tail.At;
        // The new tail is initially stationary, so normalizing its travel would yield NaN.
        newTail.Direction = tail.PreviousDirection;
        newTail.PreviousDirection = newTail.Direction;
        _segments.Add(newTail);
    }

    public void Update(TimeSpan elapsed)
    {
        _movementTimer += elapsed;
        ElapsedTime += elapsed;

        while (_movementTimer >= s_movementTime)
        {
            _movementTimer -= s_movementTime;
            Move();
        }

        _movementProgress = (float)(_movementTimer.TotalSeconds / s_movementTime.TotalSeconds);
    }

    public Vector2 GetHeadPosition()
    {
        SnakeSegment head = _segments[0];
        return Vector2.Lerp(head.At, head.To, _movementProgress);
    }

    private void Move()
    {
        if (_inputBuffer.Count > 0)
        {
            _nextDirection = _inputBuffer.Dequeue();
        }

        SnakeSegment head = _segments[0];
        head.PreviousDirection = head.Direction;
        head.Direction = _nextDirection;
        head.At = head.To;
        head.To = head.At + head.Direction * _stride;
        _segments.Insert(0, head);
        _segments.RemoveAt(_segments.Count - 1);

        for (int i = 1; i < _segments.Count; ++i)
        {
            SnakeSegment segment = _segments[i];

            if (head.At == segment.At)
            {
                BodyCollision?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }
}
