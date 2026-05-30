using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class OutputNamingService : IOutputNamingService
{
    private readonly IFileSystemService _fileSystem;

    public OutputNamingService(IFileSystemService fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public OutputPathResult CreateOutputPath(
        string sourcePath,
        string? relativePath,
        OutputFormat format,
        ConversionBatchOptions options,
        string? suffix = null,
        bool isPattern = false)
    {
        var outputFolder = ResolveOutputFolder(sourcePath, relativePath, options);
        _fileSystem.CreateDirectory(outputFolder);

        var sourceBaseName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = ToExtension(format);
        var fileName = isPattern
            ? $"{sourceBaseName}{suffix ?? "_frame"}_%04d{extension}"
            : $"{sourceBaseName}{suffix ?? string.Empty}{extension}";

        var outputPath = Path.Combine(outputFolder, fileName);
        if (isPattern)
        {
            return new OutputPathResult(outputPath);
        }

        if (!_fileSystem.FileExists(outputPath) || options.CollisionBehavior == CollisionBehavior.Overwrite)
        {
            return new OutputPathResult(outputPath);
        }

        if (options.CollisionBehavior == CollisionBehavior.Skip)
        {
            return new OutputPathResult(outputPath, true, $"Skipped because {Path.GetFileName(outputPath)} already exists.");
        }

        return new OutputPathResult(CreateAutoRenamedPath(outputFolder, sourceBaseName, suffix, extension));
    }

    public static string ToExtension(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Jpg => ".jpg",
            OutputFormat.Jpeg => ".jpeg",
            OutputFormat.Png => ".png",
            OutputFormat.Mp4 => ".mp4",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private string ResolveOutputFolder(string sourcePath, string? relativePath, ConversionBatchOptions options)
    {
        if (options.OutputFolderMode == OutputFolderMode.SameFolderAsSource)
        {
            return Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory;
        }

        var root = string.IsNullOrWhiteSpace(options.CustomOutputFolder)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Apple Converter")
            : options.CustomOutputFolder;

        if (!options.KeepFolderStructure || string.IsNullOrWhiteSpace(relativePath))
        {
            return root;
        }

        var relativeDirectory = Path.GetDirectoryName(relativePath);
        return string.IsNullOrWhiteSpace(relativeDirectory)
            ? root
            : Path.Combine(root, relativeDirectory);
    }

    private string CreateAutoRenamedPath(string outputFolder, string sourceBaseName, string? suffix, string extension)
    {
        for (var i = 1; i < 10_000; i++)
        {
            var candidate = Path.Combine(outputFolder, $"{sourceBaseName}{suffix ?? string.Empty} ({i}){extension}");
            if (!_fileSystem.FileExists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Could not create a unique output filename after 9,999 attempts.");
    }
}
