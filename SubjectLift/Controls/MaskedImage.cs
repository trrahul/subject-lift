using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SubjectLift.Extensions;
using Point = Avalonia.Point;
using Path = Avalonia.Controls.Shapes.Path;

namespace SubjectLift.Controls;

/// <summary>
/// A custom control that displays an image with an optional mask overlay.
/// </summary>
public class MaskedImage : UserControl
{
    /// <summary>
    /// Identifies the <see cref="SourceImage"/> dependency property.
    /// </summary>
    public static readonly DirectProperty<MaskedImage, Bitmap?> SourceImageProperty =
        AvaloniaProperty.RegisterDirect<MaskedImage, Bitmap?>(
            nameof(SourceImage),
            o => o.SourceImage,
            (o, v) => o.SourceImage = v);

    /// <summary>
    /// Identifies the <see cref="ImageMask"/> dependency property.
    /// </summary>
    public static readonly DirectProperty<MaskedImage, Bitmap?> ImageMaskProperty =
        AvaloniaProperty.RegisterDirect<MaskedImage, Bitmap?>(
            nameof(ImageMask),
            o => o.ImageMask,
            (o, v) => o.ImageMask = v);

    /// <summary>
    /// Identifies the <see cref="MaskEntered"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> MaskEnteredEvent =
        RoutedEvent.Register<MaskedImage, RoutedEventArgs>(nameof(MaskEntered), RoutingStrategies.Bubble);

    /// <summary>
    /// Identifies the <see cref="MaskExited"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> MaskExitedEvent =
        RoutedEvent.Register<MaskedImage, RoutedEventArgs>(nameof(MaskExited), RoutingStrategies.Bubble);

    /// <summary>
    /// Identifies the <see cref="CopyCommand"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand> CopyCommandProperty =
        AvaloniaProperty.Register<MaskedImage, ICommand>(nameof(CopyCommand));

    /// <summary>
    /// Identifies the <see cref="SaveCommand"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand> SaveCommandProperty =
        AvaloniaProperty.Register<MaskedImage, ICommand>(nameof(SaveCommand));

    /// <summary>
    /// Identifies the <see cref="MaskClicked"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<MaskClickedEventArgs> MaskClickedEvent =
        RoutedEvent.Register<MaskedImage, MaskClickedEventArgs>(nameof(MaskClicked), RoutingStrategies.Bubble);

    /// <summary>
    /// Identifies the <see cref="ButtonsHidden"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ButtonsHiddenEvent =
        RoutedEvent.Register<MaskedImage, RoutedEventArgs>(nameof(ButtonsHidden), RoutingStrategies.Bubble);

    private readonly Canvas _buttonCanvas;
    private readonly Grid _mainGrid;
    private Button _copyButton;
    private Bitmap? _imageMask;
    private Path? _maskPath;
    private Button _saveButton;
    private readonly ScrollViewer _scrollViewer;
    private Bitmap? _sourceImage;
    private readonly Viewbox _viewbox;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedImage"/> class.
    /// </summary>
    public MaskedImage()
    {
        _mainGrid = new Grid();
        _buttonCanvas = new Canvas();
        _viewbox = new Viewbox { Stretch = Stretch.Uniform };
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _viewbox
        };

        Content = new Grid
        {
            Children =
            {
                _scrollViewer,
                _buttonCanvas
            }
        };

        this.GetObservable(SourceImageProperty).Subscribe(_ => UpdateDisplay());
        this.GetObservable(ImageMaskProperty).Subscribe(_ => UpdateDisplay());

        CreateButtons();
    }

    /// <summary>
    /// Gets or sets the source image.
    /// </summary>
    public Bitmap? SourceImage
    {
        get => _sourceImage;
        set => SetAndRaise(SourceImageProperty, ref _sourceImage, value);
    }

    /// <summary>
    /// Gets or sets the image mask.
    /// </summary>
    public Bitmap? ImageMask
    {
        get => _imageMask;
        set => SetAndRaise(ImageMaskProperty, ref _imageMask, value);
    }

    /// <summary>
    /// Gets or sets the command to copy the image.
    /// </summary>
    public ICommand CopyCommand
    {
        get => GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to save the image.
    /// </summary>
    public ICommand SaveCommand
    {
        get => GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    private Image _originalImage;

    /// <summary>
    /// Updates the display of the control.
    /// </summary>
    private void UpdateDisplay()
    {
        _mainGrid.Children.Clear();

        if (SourceImage == null)
            return;

        _originalImage = new Image
        {
            Source = SourceImage,
            Stretch = Stretch.None
        };

        _originalImage.PointerPressed += OriginalImage_PointerPressed;

        _mainGrid.Children.Add(_originalImage);

        if (ImageMask != null)
        {
            var geometry = ImageMask.CreateGeometry();

            CreateGlowPath(geometry, 8, 0.1);
            CreateGlowPath(geometry, 6, 0.2);
            CreateGlowPath(geometry, 4, 0.3);
            CreateGlowPath(geometry, 2, 0.4);

            _maskPath = new Path
            {
                Data = geometry,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
            };

            _mainGrid.Children.Add(_maskPath);

            _maskPath.PointerEntered += MaskPath_PointerEntered;
            _maskPath.PointerExited += MaskPath_PointerExited;
            _maskPath.PointerPressed += MaskPath_PointerPressed;
        }

        _viewbox.Child = _mainGrid;
        _scrollViewer.Width = SourceImage.PixelSize.Width;
        _scrollViewer.Height = SourceImage.PixelSize.Height;

        PointerPressed += MaskedImage_PointerPressed;
    }

    /// <summary>
    /// Creates the copy and save buttons.
    /// </summary>
    private void CreateButtons()
    {
        _copyButton = new Button
        {
            Content = "Copy",
            Width = 80,
            Height = 30
        };
        _copyButton.Click += CopyButton_Click;

        _saveButton = new Button
        {
            Content = "Save",
            Width = 80,
            Height = 30
        };
        _saveButton.Click += SaveButton_Click;

        _buttonCanvas.Children.Add(_copyButton);
        _buttonCanvas.Children.Add(_saveButton);

        HideButtons();
    }

    /// <summary>
    /// Handles the PointerEntered event for the mask path.
    /// </summary>
    private void MaskPath_PointerEntered(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(_scrollViewer);
        _maskPath.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        RaiseEvent(new MaskClickedEventArgs(MaskClickedEvent, this, position));
        e.Handled = true;
    }

    /// <summary>
    /// Handles the PointerExited event for the mask path.
    /// </summary>
    private void MaskPath_PointerExited(object? sender, PointerEventArgs e)
    {
        _maskPath.Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        RaiseEvent(new RoutedEventArgs(MaskExitedEvent));
    }

    /// <summary>
    /// Creates a glow effect around the mask path.
    /// </summary>
    /// <param name="geometry">The geometry of the mask path.</param>
    /// <param name="thickness">The thickness of the glow.</param>
    /// <param name="opacity">The opacity of the glow.</param>
    private void CreateGlowPath(Geometry geometry, double thickness, double opacity)
    {
        var glowPath = new Path
        {
            Data = geometry,
            Stroke = new SolidColorBrush(Colors.White, opacity),
            StrokeThickness = thickness,
            Fill = null
        };
        _mainGrid.Children.Add(glowPath);
    }

    /// <summary>
    /// Handles the PointerPressed event for the mask path.
    /// </summary>
    private void MaskPath_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        ShowButtons(position);
        RaiseEvent(new MaskClickedEventArgs(MaskClickedEvent, this, position));
        e.Handled = true;
    }

    /// <summary>
    /// Handles the PointerPressed event for the control.
    /// </summary>
    private void MaskedImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        HideButtons();
    }

    /// <summary>
    /// Shows the copy and save buttons at the specified position.
    /// </summary>
    /// <param name="position">The position to show the buttons.</param>
    private void ShowButtons(Point position)
    {
        Canvas.SetLeft(_copyButton, position.X + _scrollViewer.Offset.X);
        Canvas.SetTop(_copyButton, position.Y + _scrollViewer.Offset.Y);
        Canvas.SetLeft(_saveButton, position.X + _scrollViewer.Offset.X);
        Canvas.SetTop(_saveButton, position.Y + _scrollViewer.Offset.Y + 40);

        _copyButton.IsVisible = true;
        _saveButton.IsVisible = true;
    }

    /// <summary>
    /// Hides the copy and save buttons.
    /// </summary>
    private void HideButtons()
    {
        _copyButton.IsVisible = false;
        _saveButton.IsVisible = false;
        RaiseEvent(new RoutedEventArgs(ButtonsHiddenEvent));
    }

    /// <summary>
    /// Handles the Click event for the copy button.
    /// </summary>
    private void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        CopyCommand?.Execute(null);
        HideButtons();
    }

    /// <summary>
    /// Handles the Click event for the save button.
    /// </summary>
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveCommand?.Execute(null);
        HideButtons();
    }

    /// <summary>
    /// Handles the PointerPressed event for the original image.
    /// </summary>
    private void OriginalImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var clickPosition = e.GetPosition(_originalImage);
        var pixelPosition = GetPixelPosition(clickPosition);
        RaiseEvent(new ImageClickedEventArgs(ImageClickedEvent, this, clickPosition, pixelPosition));
        e.Handled = true;
    }

    /// <summary>
    /// Gets the pixel position of a click relative to the original image.
    /// </summary>
    /// <param name="clickPosition">The click position.</param>
    /// <returns>The pixel position.</returns>
    private Point GetPixelPosition(Point clickPosition)
    {
        if (SourceImage == null || _originalImage == null) return new Point(0, 0);

        var imageRect = _originalImage.Bounds;

        var scaleX = SourceImage.PixelSize.Width / imageRect.Width;
        var scaleY = SourceImage.PixelSize.Height / imageRect.Height;

        var scrollOffset = _scrollViewer.Offset;

        var adjustedClickX = clickPosition.X + scrollOffset.X;
        var adjustedClickY = clickPosition.Y + scrollOffset.Y;

        var relativeX = adjustedClickX - imageRect.X;
        var relativeY = adjustedClickY - imageRect.Y;

        var pixelX = relativeX * scaleX;
        var pixelY = relativeY * scaleY;

        return new Point(Math.Floor(pixelX), Math.Floor(pixelY));
    }

    /// <summary>
    /// Identifies the <see cref="ImageClicked"/> routed event.
    /// </summary>
    public static readonly RoutedEvent<ImageClickedEventArgs> ImageClickedEvent =
        RoutedEvent.Register<MaskedImage, ImageClickedEventArgs>(nameof(ImageClicked), RoutingStrategies.Bubble);

    /// <summary>
    /// Occurs when the image is clicked.
    /// </summary>
    public event EventHandler<ImageClickedEventArgs> ImageClicked
    {
        add => AddHandler(ImageClickedEvent, value);
        remove => RemoveHandler(ImageClickedEvent, value);
    }

    /// <summary>
    /// Occurs when the mask is entered.
    /// </summary>
    public event EventHandler<RoutedEventArgs> MaskEntered
    {
        add => AddHandler(MaskEnteredEvent, value);
        remove => RemoveHandler(MaskEnteredEvent, value);
    }

    /// <summary>
    /// Occurs when the mask is exited.
    /// </summary>
    public event EventHandler<RoutedEventArgs> MaskExited
    {
        add => AddHandler(MaskExitedEvent, value);
        remove => RemoveHandler(MaskExitedEvent, value);
    }

    /// <summary>
    /// Occurs when the mask is clicked.
    /// </summary>
    public event EventHandler<MaskClickedEventArgs> MaskClicked
    {
        add => AddHandler(MaskClickedEvent, value);
        remove => RemoveHandler(MaskClickedEvent, value);
    }

    /// <summary>
    /// Occurs when the buttons are hidden.
    /// </summary>
    public event EventHandler<RoutedEventArgs> ButtonsHidden
    {
        add => AddHandler(ButtonsHiddenEvent, value);
        remove => RemoveHandler(ButtonsHiddenEvent, value);
    }
}