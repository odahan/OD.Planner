using System.Windows;

namespace OD.Planner.Logic;

/// <summary>
/// Calculates the position of a popup relative to its owner window.
/// </summary>
public static class PopupPlacementCalculator
{
    /// <summary>
    /// Places a popup at the bottom-right of its owner while keeping it attached to the
    /// owner's nearest edge when the popup is larger than the available inner area.
    /// </summary>
    /// <param name="ownerBounds">The owner window bounds in device-independent pixels.</param>
    /// <param name="popupSize">The measured popup size in device-independent pixels.</param>
    /// <param name="margin">The preferred inner margin from the owner edges.</param>
    /// <returns>The popup's top-left position.</returns>
    public static Point CalculateBottomRight(Rect ownerBounds, Size popupSize, double margin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(margin);

        var left = popupSize.Width + (margin * 2) <= ownerBounds.Width
            ? ownerBounds.Right - popupSize.Width - margin
            : ownerBounds.Right - popupSize.Width;

        var top = popupSize.Height + (margin * 2) <= ownerBounds.Height
            ? ownerBounds.Bottom - popupSize.Height - margin
            : ownerBounds.Top;

        return new Point(left, top);
    }
}
