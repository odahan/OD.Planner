using System.Windows;

namespace OD.Planner.Logic;

/// <summary>
/// Calculates the position of a popup relative to its owner and monitor work area.
/// </summary>
public static class PopupPlacementCalculator
{
    /// <summary>
    /// Places a popup at the bottom-right of its owner and keeps the entire popup inside
    /// the monitor work area.
    /// </summary>
    /// <param name="ownerBounds">The owner window bounds.</param>
    /// <param name="popupSize">The measured popup size.</param>
    /// <param name="workArea">The monitor work area containing the owner.</param>
    /// <param name="margin">The preferred inner margin from the owner edges.</param>
    /// <returns>The popup's top-left position.</returns>
    public static Point CalculateBottomRight(Rect ownerBounds, Size popupSize, Rect workArea, double margin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(margin);

        var desiredLeft = ownerBounds.Right - popupSize.Width - margin;
        var desiredTop = ownerBounds.Bottom - popupSize.Height - margin;

        var minimumLeft = workArea.Left + margin;
        var maximumLeft = workArea.Right - popupSize.Width - margin;
        var minimumTop = workArea.Top + margin;
        var maximumTop = workArea.Bottom - popupSize.Height - margin;

        var left = maximumLeft >= minimumLeft
            ? Math.Clamp(desiredLeft, minimumLeft, maximumLeft)
            : workArea.Left;

        var top = maximumTop >= minimumTop
            ? Math.Clamp(desiredTop, minimumTop, maximumTop)
            : workArea.Top;

        return new Point(left, top);
    }
}
