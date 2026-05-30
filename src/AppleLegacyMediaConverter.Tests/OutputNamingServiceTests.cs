using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class OutputNamingServiceTests
{
    [Fact]
    public void AutoRenameAddsIncrementWhenOutputExists()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.Files.Add(@"C:\Out\IMG_1234.jpg");
        var service = new OutputNamingService(fileSystem);

        var result = service.CreateOutputPath(
            @"C:\Photos\IMG_1234.HEIC",
            null,
            OutputFormat.Jpg,
            new ConversionBatchOptions
            {
                OutputFolderMode = OutputFolderMode.CustomFolder,
                CustomOutputFolder = @"C:\Out",
                CollisionBehavior = CollisionBehavior.AutoRename
            });

        Assert.Equal(@"C:\Out\IMG_1234 (1).jpg", result.Path);
        Assert.False(result.ShouldSkip);
    }

    [Fact]
    public void SkipCollisionReturnsSkipResult()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.Files.Add(@"C:\Out\IMG_1234.png");
        var service = new OutputNamingService(fileSystem);

        var result = service.CreateOutputPath(
            @"C:\Photos\IMG_1234.HEIC",
            null,
            OutputFormat.Png,
            new ConversionBatchOptions
            {
                OutputFolderMode = OutputFolderMode.CustomFolder,
                CustomOutputFolder = @"C:\Out",
                CollisionBehavior = CollisionBehavior.Skip
            });

        Assert.True(result.ShouldSkip);
        Assert.Contains("đã tồn tại", result.SkipReason);
    }

    [Fact]
    public void KeepsRelativeFolderStructureForFolderAdds()
    {
        var service = new OutputNamingService(new FakeFileSystem());

        var result = service.CreateOutputPath(
            @"C:\Photos\Vacation\IMG_1234.HEIC",
            @"Vacation\IMG_1234.HEIC",
            OutputFormat.Jpeg,
            new ConversionBatchOptions
            {
                OutputFolderMode = OutputFolderMode.CustomFolder,
                CustomOutputFolder = @"C:\Out",
                KeepFolderStructure = true
            });

        Assert.Equal(@"C:\Out\Vacation\IMG_1234.jpeg", result.Path);
    }

    private sealed class FakeFileSystem : IFileSystemService
    {
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool FileExists(string path) => Files.Contains(path);

        public bool DirectoryExists(string path) => true;

        public void CreateDirectory(string path)
        {
        }

        public IEnumerable<string> EnumerateFiles(string folderPath, bool recursive) => Enumerable.Empty<string>();

        public DateTime GetCreationTimeUtc(string path) => DateTime.UnixEpoch;

        public DateTime GetLastWriteTimeUtc(string path) => DateTime.UnixEpoch;

        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
        }

        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
        }
    }
}
