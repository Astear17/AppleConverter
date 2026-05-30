using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task SavesAndLoadsSettingsAsJson()
    {
        var path = Path.Combine(Path.GetTempPath(), "AppleConverter.Tests", Guid.NewGuid().ToString("N"), "settings.json");
        var service = new JsonFileSettingsService(path);
        var settings = new AppSettings
        {
            Theme = AppTheme.Dark,
            DefaultImageOutputFormat = OutputFormat.Png,
            JpegQuality = 77,
            CollisionBehavior = CollisionBehavior.Skip,
            FFmpegPath = @"C:\Tools\ffmpeg.exe"
        };

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal(AppTheme.Dark, loaded.Theme);
        Assert.Equal(OutputFormat.Png, loaded.DefaultImageOutputFormat);
        Assert.Equal(77, loaded.JpegQuality);
        Assert.Equal(CollisionBehavior.Skip, loaded.CollisionBehavior);
        Assert.Equal(@"C:\Tools\ffmpeg.exe", loaded.FFmpegPath);
    }
}
