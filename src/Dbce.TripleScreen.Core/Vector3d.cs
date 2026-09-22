using System;

namespace Dbce.TripleScreen;

/// <summary>A small dependency-free vector type for physical display geometry.</summary>
public readonly struct Vector3d : IEquatable<Vector3d>
{
    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public double Length => Math.Sqrt(Dot(this, this));

    public Vector3d Normalized()
    {
        var length = Length;
        if (length <= 1e-12)
        {
            throw new InvalidOperationException("A zero-length vector cannot be normalized.");
        }

        return this / length;
    }

    public static double Dot(Vector3d left, Vector3d right) =>
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    public static Vector3d Cross(Vector3d left, Vector3d right) => new(
        (left.Y * right.Z) - (left.Z * right.Y),
        (left.Z * right.X) - (left.X * right.Z),
        (left.X * right.Y) - (left.Y * right.X));

    public static Vector3d operator +(Vector3d left, Vector3d right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3d operator -(Vector3d left, Vector3d right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3d operator -(Vector3d value) => new(-value.X, -value.Y, -value.Z);
    public static Vector3d operator *(Vector3d value, double scale) => new(value.X * scale, value.Y * scale, value.Z * scale);
    public static Vector3d operator /(Vector3d value, double scale) => new(value.X / scale, value.Y / scale, value.Z / scale);

    public bool Equals(Vector3d other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Vector3d other && Equals(other);
    public override int GetHashCode() => ((X.GetHashCode() * 397) ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode();
    public override string ToString() => $"({X:F6}, {Y:F6}, {Z:F6})";
}
