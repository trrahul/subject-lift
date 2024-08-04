using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using Point = Avalonia.Point;

namespace SubjectLift.Extensions;

public static class ImageExtensions
{
    /// <summary>
    /// Applies a mask to the original image and optionally crops the result.
    /// </summary>
    /// <param name="originalImg">The original image as an OpenCV Mat.</param>
    /// <param name="maskImg">The mask image as an OpenCV Mat.</param>
    /// <param name="crop">Whether to crop the result to the bounding box of the mask.</param>
    /// <returns>The masked image as an OpenCV Mat.</returns>
    /// <exception cref="Exception">Thrown if the sizes of the original image and mask image do not match.</exception>
    public static Mat GetMaskedResult(this Mat originalImg, Mat maskImg, bool crop = false)
    {
        if (originalImg.Size() != maskImg.Size())
            throw new Exception("Original image and mask image sizes do not match.");

        // Convert the original image to BGRA if it's not already
        var bgraImg = originalImg.Channels() == 4 ? originalImg : new Mat();
        if (originalImg.Channels() == 3)
            Cv2.CvtColor(originalImg, bgraImg, ColorConversionCodes.BGR2BGRA);
        else if (originalImg.Channels() != 4) throw new Exception("Original image must have 3 or 4 channels.");

        var maskedImg = new Mat(bgraImg.Size(), MatType.CV_8UC4);
        for (var y = 0; y < maskImg.Rows; y++)
        for (var x = 0; x < maskImg.Cols; x++)
            if (maskImg.At<byte>(y, x) > 0)
                maskedImg.Set(y, x, bgraImg.At<Vec4b>(y, x));
            else
                maskedImg.Set(y, x, new Vec4b(0, 0, 0, 0)); // Set background to transparent

        if (crop)
        {
            var boundingBox = Cv2.BoundingRect(maskImg);
            var croppedMaskedImg = new Mat(boundingBox.Size, MatType.CV_8UC4);
            maskedImg.SubMat(boundingBox).CopyTo(croppedMaskedImg);
            return croppedMaskedImg;
        }

        return maskedImg;
    }

    /// <summary>
    /// Converts an Avalonia WriteableBitmap to an OpenCV Mat.
    /// </summary>
    /// <param name="bitmap">The WriteableBitmap to convert.</param>
    /// <returns>The converted Mat.</returns>
    public static Mat ToMat(this WriteableBitmap bitmap)
    {
        using var lockedBitmap = bitmap.Lock();
        var stride = lockedBitmap.RowBytes;
        var height = lockedBitmap.Size.Height;
        var width = lockedBitmap.Size.Width;
        var pixelData = new byte[stride * height];
        Marshal.Copy(lockedBitmap.Address, pixelData, 0, pixelData.Length);

        var mat = new Mat(height, width, MatType.CV_8UC4);

        Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);

        return mat;
    }

    /// <summary>
    /// Converts a single-channel (grayscale) Mat to a 4-channel BGRA Avalonia Bitmap.
    /// </summary>
    /// <param name="mat">The single-channel Mat to convert.</param>
    /// <returns>The converted Bitmap.</returns>
    /// <exception cref="ArgumentException">Thrown if the Mat is not a single-channel image.</exception>
    public static Bitmap To1ChannelBitmap(this Mat mat)
    {
        if (mat.Channels() != 1)
        {
            throw new ArgumentException("The Mat must be a single-channel (grayscale) image.");
        }

        // Convert the single-channel Mat to a 4-channel BGRA image
        Mat bgraMat = new Mat();
        Cv2.CvtColor(mat, bgraMat, ColorConversionCodes.GRAY2BGRA);

        // Create a byte array to hold the image data
        byte[] imageData = new byte[bgraMat.Cols * bgraMat.Rows * 4];
        Marshal.Copy(bgraMat.Data, imageData, 0, imageData.Length);

        // Create a new Avalonia Bitmap
        var bitmap = new Bitmap(
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul,
            bgraMat.Data,
            new Avalonia.PixelSize(bgraMat.Cols, bgraMat.Rows),
            new Avalonia.Vector(96, 96),
            bgraMat.Cols * 4);

        bgraMat.Dispose();
        return bitmap;
    }

    /// <summary>
    /// Creates a Geometry object from a mask Bitmap.
    /// </summary>
    /// <param name="maskBitmap">The mask Bitmap.</param>
    /// <returns>The created Geometry object.</returns>
    public static Geometry CreateGeometry(this Bitmap maskBitmap)
    {
        using var memoryStream = new MemoryStream();
        maskBitmap.Save(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var mat = Mat.FromStream(memoryStream, ImreadModes.Grayscale);
        Cv2.Threshold(mat, mat, 127, 255, ThresholdTypes.Binary);

        Cv2.FindContours(mat, out var contours, out _, RetrievalModes.External,
            ContourApproximationModes.ApproxNone);

        var geometryGroup = new GeometryGroup();

        foreach (var contour in contours)
        {
            var points = new List<Point>();
            foreach (var cvPoint in contour) points.Add(new Point(cvPoint.X, cvPoint.Y));

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure {IsClosed = true, StartPoint = points[0]};
            pathFigure.Segments.Add(new PolyLineSegment(points));
            pathGeometry.Figures.Add(pathFigure);

            geometryGroup.Children.Add(pathGeometry);
        }

        return geometryGroup;
    }
}