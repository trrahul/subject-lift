namespace SubjectLift.Processing;

/// <summary>
/// Configuration class for the Segment Anything Model.
/// </summary>
public class SegmentAnythingModelConfig
{
    /// <summary>
    /// Gets or sets the path to the decoder model file.
    /// </summary>
    public string DecoderPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the encoder model file.
    /// </summary>
    public string EncoderPath { get; set; }
}