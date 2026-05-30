using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface IBackendStatusService
{
    Task<BackendStatus> GetStatusAsync(AppSettings settings, CancellationToken cancellationToken = default);

    string? FindFFmpegPath(AppSettings settings);
}
