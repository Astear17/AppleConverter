using AppleLegacyMediaConverter.Core.Interfaces;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class LocalFileSystemService : IFileSystemService
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public IEnumerable<string> EnumerateFiles(string folderPath, bool recursive)
    {
        if (!Directory.Exists(folderPath))
        {
            return Enumerable.Empty<string>();
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(folderPath, "*", option);
    }

    public DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => File.SetCreationTimeUtc(path, creationTimeUtc);

    public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
}
