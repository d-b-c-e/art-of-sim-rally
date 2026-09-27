using System;

namespace Dbce.TripleScreen;

public readonly struct SpanViewport
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }

    internal SpanViewport(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

public static class TripleViewportLayout
{
    public static SpanViewport ForPanelIndex(int index)
    {
        if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
        return new SpanViewport(index / 3d, 0d, 1d / 3d, 1d);
    }
}
