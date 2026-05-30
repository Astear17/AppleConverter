namespace AppleLegacyMediaConverter.Core.Models;

public sealed record BackendStatus(
    bool FFmpegFound,
    string? FFmpegPath,
    string FFmpegMessage,
    bool ImageBackendAvailable,
    string ImageBackendMessage,
    IReadOnlyList<string> SupportedImageInputs,
    IReadOnlyList<string> SupportedVideoInputs,
    IReadOnlyList<string> SupportedOutputs)
{
    public bool IsReadyForVideo => FFmpegFound;

    public string ToDiagnosticText()
    {
        return string.Join(
            Environment.NewLine,
            "Apple Converter diagnostics",
            $"FFmpeg found: {FFmpegFound}",
            $"FFmpeg path: {FFmpegPath ?? "(not set)"}",
            $"FFmpeg status: {FFmpegMessage}",
            $"Image backend: {(ImageBackendAvailable ? "Available" : "Missing")}",
            $"Image backend status: {ImageBackendMessage}",
            $"Image inputs: {string.Join(", ", SupportedImageInputs)}",
            $"Video inputs: {string.Join(", ", SupportedVideoInputs)}",
            $"Outputs: {string.Join(", ", SupportedOutputs)}");
    }
}
