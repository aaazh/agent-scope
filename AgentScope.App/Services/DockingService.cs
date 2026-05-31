using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace AgentScope.App.Services;

/// <summary>
/// Handles magnetic edge docking for the floating window.
/// </summary>
public class DockingService
{
    private readonly Window _window;
    private const int SnapThreshold = 15;
    private const int DetachThreshold = 30;

    // Track the edge we're snapped to for hysteresis
    private DockedEdge _snappedEdge = DockedEdge.None;

    [Flags]
    private enum DockedEdge
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8
    }

    public DockingService(Window window)
    {
        _window = window;
    }

    /// <summary>
    /// Check proximity to screen edges and snap if within threshold.
    /// Called on LocationChanged during drag.
    /// </summary>
    public void CheckDocking()
    {
        var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(_window).Handle);
        var workArea = screen.WorkingArea;
        var windowRect = new System.Drawing.Rectangle(
            (int)_window.Left,
            (int)_window.Top,
            (int)_window.ActualWidth,
            (int)_window.ActualHeight);

        // If currently snapped, check if we've dragged past detach threshold
        if (_snappedEdge != DockedEdge.None)
        {
            if (!ShouldDetach(windowRect, workArea))
                return; // Stay snapped
            _snappedEdge = DockedEdge.None;
        }

        // Check proximity to each edge (priority: top > right > bottom > left)
        if (Math.Abs(windowRect.Top - workArea.Top) <= SnapThreshold)
        {
            _window.Top = workArea.Top;
            _snappedEdge = DockedEdge.Top;
        }
        else if (Math.Abs(windowRect.Right - workArea.Right) <= SnapThreshold)
        {
            _window.Left = workArea.Right - _window.ActualWidth;
            _snappedEdge = DockedEdge.Right;
        }
        else if (Math.Abs(windowRect.Bottom - workArea.Bottom) <= SnapThreshold)
        {
            _window.Top = workArea.Bottom - _window.ActualHeight;
            _snappedEdge = DockedEdge.Bottom;
        }
        else if (Math.Abs(windowRect.Left - workArea.Left) <= SnapThreshold)
        {
            _window.Left = workArea.Left;
            _snappedEdge = DockedEdge.Left;
        }
    }

    private bool ShouldDetach(System.Drawing.Rectangle windowRect, System.Drawing.Rectangle workArea)
    {
        return _snappedEdge switch
        {
            DockedEdge.Top => Math.Abs(windowRect.Top - workArea.Top) > DetachThreshold,
            DockedEdge.Right => Math.Abs(windowRect.Right - workArea.Right) > DetachThreshold,
            DockedEdge.Bottom => Math.Abs(windowRect.Bottom - workArea.Bottom) > DetachThreshold,
            DockedEdge.Left => Math.Abs(windowRect.Left - workArea.Left) > DetachThreshold,
            _ => true
        };
    }
}
