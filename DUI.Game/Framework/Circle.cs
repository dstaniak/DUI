using System;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary;

public readonly struct Circle : IEquatable<Circle>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Radius;

    public readonly Point Location => new Point(X, Y);
    public readonly bool IsEmpty => X == 0 && Y == 0 && Radius == 0;

    public readonly int Top => Y - Radius;
    public readonly int Bottom => Y + Radius;
    public readonly int Left => X - Radius;
    public readonly int Right => X + Radius;

    public static Circle Empty => s_empty;

    private static readonly Circle s_empty = new(); // Reusable memory

    public Circle(int x, int y, int radius)
    {
        X = x;
        Y = y;
        Radius = radius;
    }

    public Circle(Point position, int radius)
    {
        X = position.X;
        Y = position.Y;
        Radius = radius;
    }

    public bool Intersects(Circle other)
    {
        int radiiSum = Radius + other.Radius;
        int radiiSquared = radiiSum * radiiSum;
        float distanceSquared = Vector2.DistanceSquared(Location.ToVector2(), other.Location.ToVector2());
        return distanceSquared < radiiSquared;
    }

    public override readonly bool Equals(object obj) => obj is Circle other && Equals(other);

    public readonly bool Equals(Circle other)
    {
        return X == other.X && Y == other.Y && Radius == other.Radius;
    }

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Radius);

    public static bool operator ==(Circle lhs, Circle rhs) => lhs.Equals(rhs);
    public static bool operator !=(Circle lhs, Circle rhs) => !lhs.Equals(rhs);
}
