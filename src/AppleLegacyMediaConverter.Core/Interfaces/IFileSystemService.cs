namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IFileSystemService
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    IEnumerable<string> EnumerateFiles(string folderPath, bool recursive);

    DateTime GetCreationTimeUtc(string path);

    DateTime GetLastWriteTimeUtc(string path);

    void SetCreationTimeUtc(string path, DateTime creationTimeUtc);

    void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);
}
