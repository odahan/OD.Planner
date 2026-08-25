using System.Windows;

namespace OD.Planner.Logic;

/// <summary>
/// Calculates the position of a popup within a monitor work area.
/// </summary>
public static class PopupPlacementCalculator
{
    /// <summary>
    /// Centers a popup over its owner and keeps it entirely within the monitor work area.
    /// When the popup is larger than the work area, its top-left corner is aligned with
    /// the work area's top-left corner.
    /// </summary>
    /// <param name="ownerBounds">The owner window bounds.</param>
    /// <param name="popupSize">The measured popup size.</param>
    /// <param name="workArea">The monitor work area containing the owner.</param>
    /// <returns>The popup's top-left position.</returns>
    public static Point CalculateCenteredOnOwner(Rect ownerBounds, Size popupSize, Rect workArea)
    {
        var desiredLeft = ownerBounds.Left + ((ownerBounds.Width - popupSize.Width) / 2);
        var desiredTop = ownerBounds.Top + ((ownerBounds.Height - popupSize.Height) / 2);

        var left = popupSize.Width <= workArea.Width
            ? Math.Clamp(desiredLeft, workArea.Left, workArea.Right - popupSize.Width)
            : workArea.Left;

        var top = popupSize.Height <= workArea.Height
            ? Math.Clamp(desiredTop, workArea.Top, workArea.Bottom - popupSize.Height)
            : workArea.Top;

        return new Point(left, top);
    }
}
