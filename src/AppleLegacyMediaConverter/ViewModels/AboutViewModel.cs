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

    public string Description => "Fast Windows conversion for Apple photos, videos, and Live Photo pairs.";

    public string GitHubLink => "https://github.com/your-org/AppleConverter";

    public string License => "License placeholder: choose and update before public release.";

    public string BackendInfo => "Images use Magick.NET. Videos use FFmpeg when ffmpeg.exe is configured or bundled.";

    public string LogsLocation { get; }
}
