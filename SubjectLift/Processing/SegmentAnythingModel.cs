using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace SubjectLift.Processing;

/// <summary>
/// Represents a model for segmenting images using the Segment Anything Model.
/// </summary>
public class SegmentAnythingModel
{
    private readonly SegmentAnythingModelConfig _modelConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentAnythingModel"/> class.
    /// </summary>
    /// <param name="modelConfig">The configuration for the model.</param>
    public SegmentAnythingModel(SegmentAnythingModelConfig modelConfig)
    {
        _modelConfig = modelConfig;
    }

    /// <summary>
    /// Gets the mask for a given image and point.
    /// </summary>
    /// <param name="imagePath">The path to the image file.</param>
    /// <param name="point">The point to segment around.</param>
    /// <returns>A mask as an OpenCV Mat object.</returns>
    public Mat GetMask(string imagePath, Point point)
    {
        // Load the image
        using var img = Cv2.ImRead(imagePath);
        Cv2.CvtColor(img, img, ColorConversionCodes.BGR2RGB);

        var origWidth = img.Width;
        var origHeight = img.Height;

        Console.WriteLine($"Original size: {origWidth}x{origHeight}");

        // Resize the image to fit within 1024x1024 while maintaining aspect ratio
        int resizedWidth, resizedHeight;
        if (origWidth > origHeight)
        {
            resizedWidth = 1024;
            resizedHeight = (int) (1024.0 / origWidth * origHeight);
        }
        else
        {
            resizedHeight = 1024;
            resizedWidth = (int) (1024.0 / origHeight * origWidth);
        }

        Cv2.Resize(img, img, new Size(resizedWidth, resizedHeight), interpolation: InterpolationFlags.Linear);

        Console.WriteLine($"Resized: {resizedWidth}x{resizedHeight}");

        // Convert the image to a tensor and normalize it
        float[] mean = {123.675f, 116.28f, 103.53f};
        float[] std = {58.395f, 57.12f, 57.375f};

        var inputTensor = new DenseTensor<float>(new[] {1, 3, 1024, 1024});

        for (var y = 0; y < resizedHeight; y++)
        for (var x = 0; x < resizedWidth; x++)
        {
            var pixel = img.At<Vec3b>(y, x);
            for (var c = 0; c < 3; c++)
            {
                var normalizedValue = (pixel[c] - mean[c]) / std[c];
                inputTensor[0, c, y, x] = normalizedValue;
            }
        }

        // Pad the tensor if necessary to make it 1024x1024
        if (resizedHeight < 1024 || resizedWidth < 1024)
        {
            var paddedTensor = new DenseTensor<float>(new[] {1, 3, 1024, 1024});
            for (var c = 0; c < 3; c++)
            for (var y = 0; y < resizedHeight; y++)
            for (var x = 0; x < resizedWidth; x++)
                paddedTensor[0, c, y, x] = inputTensor[0, c, y, x];
            inputTensor = paddedTensor;
        }

        // Run the encoder model
        using var encoderSession = new InferenceSession(_modelConfig.EncoderPath);

        var inputName = encoderSession.InputMetadata.Keys.First();

        var encoderInputs = new List<NamedOnnxValue> {NamedOnnxValue.CreateFromTensor(inputName, inputTensor)};
        using var encoderResults = encoderSession.Run(encoderInputs);
        var embeddings = encoderResults.First().AsTensor<float>();

        // Encode the prompt
        float[] inputPoint = {point.X, point.Y};
        float[] inputLabel = {1};

        var onnxCoord = new DenseTensor<float>(new[] {1, 2, 2})
        {
            [0, 0, 0] = inputPoint[0] * (resizedWidth / (float) origWidth),
            [0, 0, 1] = inputPoint[1] * (resizedHeight / (float) origHeight),
            [0, 1, 0] = 0,
            [0, 1, 1] = 0
        };

        var onnxLabel = new DenseTensor<float>(new[] {1, 2})
        {
            [0, 0] = inputLabel[0],
            [0, 1] = -1
        };

        // Prepare inputs for the decoder model
        var onnxMaskInput = new DenseTensor<float>(new[] {1, 1, 256, 256});
        var onnxHasMaskInput = new DenseTensor<float>(new[] {1});

        // Load and run the decoder model
        using var decoderSession = new InferenceSession(_modelConfig.DecoderPath);
        var decoderInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("image_embeddings", embeddings),
            NamedOnnxValue.CreateFromTensor("point_coords", onnxCoord),
            NamedOnnxValue.CreateFromTensor("point_labels", onnxLabel),
            NamedOnnxValue.CreateFromTensor("mask_input", onnxMaskInput),
            NamedOnnxValue.CreateFromTensor("has_mask_input", onnxHasMaskInput),
            NamedOnnxValue.CreateFromTensor("orig_im_size",
                new DenseTensor<float>(new[] {2}) {[0] = origHeight, [1] = origWidth})
        };

        using var decoderResults = decoderSession.Run(decoderInputs);
        var masks = decoderResults.First().AsTensor<float>();

        // Post-process and visualize the mask
        var mask = new Mat(origHeight, origWidth, MatType.CV_8UC1);
        var masksArray = masks.ToArray();

        for (var y = 0; y < origHeight; y++)
        for (var x = 0; x < origWidth; x++)
        {
            var index = y * origWidth + x;
            var value = masksArray[index] > 0 ? (byte) 255 : (byte) 0;
            mask.Set(y, x, value);
        }

        return mask;
    }
}