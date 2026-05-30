using System.Reflection;
using AppleLegacyMediaConverter.Core.Interfaces;

namespace AppleLegacyMediaConverter.ViewModels;

public sealed class AboutViewModel
{
    public AboutViewModel(ILoggerService logger)
    {
        LogsLocation = logger.LogDirectory;
    }

    public string AppName => "Apple Converter";

    public string Version => typeof(AboutViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.1.0-dev";

    public string Description => "Ứng dụng Windows chuyển ảnh, video và Live Photo của Apple sang định dạng dễ mở trên thiết bị/công cụ cũ.";

    public string GitHubLink => "GitHub: https://github.com/Astear17/AppleConverter";

    public string License => "License: MIT License";

    public string BackendInfo => "Ảnh dùng Magick.NET. Video dùng FFmpeg khi ffmpeg.exe được cấu hình hoặc được đóng gói cùng ứng dụng.";

    public string LogsLocation { get; }
}
