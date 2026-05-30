using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Services;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.Models;
using AppleLegacyMediaConverter.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace AppleLegacyMediaConverter;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Directory.CreateDirectory(AppPaths.LocalDataRoot);
        Directory.CreateDirectory(AppPaths.LogsPath);
        Directory.CreateDirectory(AppPaths.DefaultOutputFolder);

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        var state = AppHost.Services.GetRequiredService<ApplicationState>();
        var settingsService = AppHost.Services.GetRequiredService<ISettingsService>();
        state.Settings = settingsService.LoadAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(state.Settings.CustomOutputFolder))
        {
            state.Settings.CustomOutputFolder = AppPaths.DefaultOutputFolder;
        }
    }

    public static IHost AppHost { get; private set; } = null!;

    public static Window? MainWindowInstance { get; private set; }

    public static T GetService<T>() where T : notnull => AppHost.Services.GetRequiredService<T>();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = AppHost.Services.GetRequiredService<MainWindow>();
        MainWindowInstance.Activate();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<ApplicationState>();
        services.AddSingleton<ISettingsService>(_ => new JsonFileSettingsService(AppPaths.SettingsPath));
        services.AddSingleton<ILoggerService>(_ => new FileLoggerService(AppPaths.LogsPath));
        services.AddSingleton<IFileSystemService, LocalFileSystemService>();
        services.AddSingleton<IFileDetectionService, FileDetectionService>();
        services.AddSingleton<ILivePhotoDetector, LivePhotoDetector>();
        services.AddSingleton<IOutputNamingService, OutputNamingService>();
        services.AddSingleton<IBackendStatusService, BackendStatusService>();
        services.AddSingleton<IImageConverterService, ImageConverterService>();
        services.AddSingleton<IVideoConverterService, VideoConverterService>();
        services.AddSingleton<IMediaConverter, MediaConverter>();
        services.AddSingleton<IConversionQueueService, ConversionQueueService>();

        services.AddTransient<ConvertViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackendStatusViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
