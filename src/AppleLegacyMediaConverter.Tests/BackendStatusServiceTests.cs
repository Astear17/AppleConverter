using AppleLegacyMediaConverter.Core.Interfaces;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Core.Services;

namespace AppleLegacyMediaConverter.Tests;

public sealed class BackendStatusServiceTests
{
    [Fact]
    public async Task MissingFfmpegReportsActionableStatus()
    {
        var service = new BackendStatusService(new MissingFileSystem(), new FileDetectionService());

        var status = await service.GetStatusAsync(new AppSettings { FFmpegPath = @"C:\Missing\ffmpeg.exe" });

        Assert.False(status.FFmpegFound);
        Assert.Contains("Không tìm thấy FFmpeg", status.FFmpegMessage);
    }

    private sealed class MissingFileSystem : IFileSystemService
    {
        public bool FileExists(string path) => false;

        public bool DirectoryExists(string path) => false;

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
