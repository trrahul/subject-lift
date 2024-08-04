using Avalonia;
using Avalonia.Interactivity;

namespace SubjectLift.Controls;

/// <summary>
/// Provides data for the ImageClicked event.
/// </summary>
public class ImageClickedEventArgs(RoutedEvent routedEvent, object? source, Point clickPosition, Point pixelPosition)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>
    /// Gets the position where the image was clicked.
    /// </summary>
    public Point ClickPosition { get; } = clickPosition;

    /// <summary>
    /// Gets the pixel position of the click relative to the original image.
    /// </summary>
    public Point PixelPosition { get; } = pixelPosition;
}