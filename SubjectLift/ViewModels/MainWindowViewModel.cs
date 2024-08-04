using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using ReactiveUI;
using SubjectLift.Extensions;
using SubjectLift.Processing;
using Point = Avalonia.Point;

namespace SubjectLift.ViewModels;

/// <summary>
/// ViewModel for the main window of the application.
/// </summary>
public class MainWindowViewModel : ReactiveObject
{
    private Bitmap? _sourceImageBitmap;
    private Bitmap? _maskImageBitmap;
    private string? _sourcePath;
    private readonly SegmentAnythingModel _model;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel()
    {
        SaveCommand = ReactiveCommand.Create(Save);
        OpenImageCommand = ReactiveCommand.Create(OpenImage);
        SegmentCommand = ReactiveCommand.Create<(Point clickPosition, Point pixelPosition)>(ExecuteSegment);
        CopyCommand = ReactiveCommand.Create(() => throw new NotImplementedException());
        _model = new SegmentAnythingModel(new SegmentAnythingModelConfig()
        {
            DecoderPath = Path.Combine("Models", "decoder.onnx"),
            EncoderPath = Path.Combine("Models", "encoder.onnx")
        });
    }

    /// <summary>
    /// Executes the segmentation process based on the provided positions.
    /// </summary>
    /// <param name="positions">A tuple containing the click position and pixel position.</param>
    private void ExecuteSegment((Point clickPosition, Point pixelPosition) positions)
    {
        var (_, pixelPosition) = positions;

        Mat mask = _model.GetMask(_sourcePath!, new OpenCvSharp.Point(pixelPosition.X, pixelPosition.Y));
        MaskImageBitmap = mask.To1ChannelBitmap();
    }

    /// <summary>
    /// Opens an image file and sets it as the source image.
    /// </summary>
    private async Task OpenImage()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Open Image",
            Filters = { new FileDialogFilter { Name = "Image", Extensions = { "png", "jpg", "jpeg" } } }
        };

        var result = await openFileDialog.ShowAsync(App.MainWindow);
        if (result != null)
        {
            var filePath = result.FirstOrDefault();
            if (filePath != null)
            {
                _sourcePath = filePath;
                SourceImageBitmap = new Bitmap(filePath);
            }
        }
    }

    /// <summary>
    /// Saves the masked image to a file.
    /// </summary>
    private async Task Save()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "Save Masked Image",
            DefaultExtension = ".png",
            Filters = { new FileDialogFilter { Name = "PNG Image", Extensions = { "png" } } }
        };

        var result = await saveFileDialog.ShowAsync(App.MainWindow);
        if (!string.IsNullOrEmpty(result))
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets or sets the source image bitmap.
    /// </summary>
    public Bitmap? SourceImageBitmap
    {
        get => _sourceImageBitmap;
        set => this.RaiseAndSetIfChanged(ref _sourceImageBitmap, value);
    }

    /// <summary>
    /// Gets or sets the mask image bitmap.
    /// </summary>
    public Bitmap? MaskImageBitmap
    {
        get => _maskImageBitmap;
        set => this.RaiseAndSetIfChanged(ref _maskImageBitmap, value);
    }

    /// <summary>
    /// Gets or sets the command to copy the image.
    /// </summary>
    public ICommand CopyCommand { get; set; }

    /// <summary>
    /// Gets or sets the command to save the image.
    /// </summary>
    public ICommand SaveCommand { get; set; }

    /// <summary>
    /// Gets the command to open an image.
    /// </summary>
    public ICommand OpenImageCommand { get; }

    /// <summary>
    /// Gets the command to segment the image.
    /// </summary>
    public ICommand SegmentCommand { get; }
}