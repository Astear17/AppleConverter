namespace AppleLegacyMediaConverter.Helpers;

public static class AppPaths
{
    public static string LocalDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Apple Converter");

    public static string SettingsPath => Path.Combine(LocalDataRoot, "settings.json");

    public static string LogsPath => Path.Combine(LocalDataRoot, "Logs");

    public static string DefaultOutputFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "Apple Converter");
}
