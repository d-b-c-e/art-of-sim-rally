using System;

namespace Dbce.TripleScreen;

public sealed class PanelDimensions
{
    private PanelDimensions(double widthMm, double heightMm)
    {
        WidthMm = widthMm;
        HeightMm = heightMm;
    }

    public double WidthMm { get; }
    public double HeightMm { get; }

    /// <summary>
    /// Estimates flat panel dimensions from marketed diagonal and aspect ratio.
    /// Prefer manufacturer active-area measurements when available.
    /// </summary>
    public static PanelDimensions FromDiagonal(double diagonalInches, double aspectWidth, double aspectHeight)
    {
        if (!(diagonalInches > 0d)) throw new ArgumentOutOfRangeException(nameof(diagonalInches));
        if (!(aspectWidth > 0d)) throw new ArgumentOutOfRangeException(nameof(aspectWidth));
        if (!(aspectHeight > 0d)) throw new ArgumentOutOfRangeException(nameof(aspectHeight));

        var diagonalMm = diagonalInches * 25.4d;
        var aspectDiagonal = Math.Sqrt((aspectWidth * aspectWidth) + (aspectHeight * aspectHeight));
        return new PanelDimensions(
            diagonalMm * aspectWidth / aspectDiagonal,
            diagonalMm * aspectHeight / aspectDiagonal);
    }
}
