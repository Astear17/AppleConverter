using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IOutputNamingService
{
    OutputPathResult CreateOutputPath(
        string sourcePath,
        string? relativePath,
        OutputFormat format,
        ConversionBatchOptions options,
        string? suffix = null,
        bool isPattern = false);
}
