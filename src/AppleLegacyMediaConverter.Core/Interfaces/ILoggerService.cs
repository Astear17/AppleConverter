namespace AppleLegacyMediaConverter.Core.Interfaces;

public interface ILoggerService
{
    string LogDirectory { get; }

    string CurrentLogPath { get; }

    Task LogInfoAsync(string message, CancellationToken cancellationToken = default);

    Task LogWarningAsync(string message, CancellationToken cancellationToken = default);

    Task LogErrorAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default);
}
