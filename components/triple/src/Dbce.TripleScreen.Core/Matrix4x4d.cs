using System;

namespace Dbce.TripleScreen;

/// <summary>A row-major 4x4 matrix independent of Unity or a UI framework.</summary>
public sealed class Matrix4x4d
{
    private readonly double[] _values;

    public Matrix4x4d(params double[] values)
    {
        if (values is null || values.Length != 16)
        {
            throw new ArgumentException("Exactly 16 row-major values are required.", nameof(values));
        }

        _values = (double[])values.Clone();
    }

    public double this[int row, int column] => _values[(row * 4) + column];
    public double[] ToRowMajorArray() => (double[])_values.Clone();
}
