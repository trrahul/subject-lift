using Avalonia;
using Avalonia.Interactivity;

namespace SubjectLift.Controls;

/// <summary>
/// Provides data for the MaskClicked event.
/// </summary>
public class MaskClickedEventArgs(RoutedEvent routedEvent, object? source, Point clickPosition)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>
    /// Gets the position where the mask was clicked.
    /// </summary>
    public Point ClickPosition { get; } = clickPosition;
}