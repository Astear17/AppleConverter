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
            "Chẩn đoán Apple Converter",
            $"Tìm thấy FFmpeg: {FFmpegFound}",
            $"Đường dẫn FFmpeg: {FFmpegPath ?? "(chưa đặt)"}",
            $"Trạng thái FFmpeg: {FFmpegMessage}",
            $"Backend ảnh: {(ImageBackendAvailable ? "Sẵn sàng" : "Thiếu")}",
            $"Trạng thái backend ảnh: {ImageBackendMessage}",
            $"Ảnh đầu vào: {string.Join(", ", SupportedImageInputs)}",
            $"Video đầu vào: {string.Join(", ", SupportedVideoInputs)}",
            $"Đầu ra: {string.Join(", ", SupportedOutputs)}");
    }
}
