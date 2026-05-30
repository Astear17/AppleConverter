using System.Text;
using AppleLegacyMediaConverter.Core.Interfaces;

namespace AppleLegacyMediaConverter.Core.Services;

public sealed class FileLoggerService : ILoggerService
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileLoggerService(string logDirectory)
    {
        LogDirectory = logDirectory;
        Directory.CreateDirectory(LogDirectory);
        CurrentLogPath = Path.Combine(LogDirectory, $"apple-converter-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public string LogDirectory { get; }

    public string CurrentLogPath { get; }

    public Task LogInfoAsync(string message, CancellationToken cancellationToken = default)
    {
        return WriteAsync("INFO", message, null, cancellationToken);
    }

    public Task LogWarningAsync(string message, CancellationToken cancellationToken = default)
    {
        return WriteAsync("WARN", message, null, cancellationToken);
    }

    public Task LogErrorAsync(string message, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        return WriteAsync("ERROR", message, exception, cancellationToken);
    }

    private async Task WriteAsync(string level, string message, Exception? exception, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder()
            .Append(DateTimeOffset.UtcNow.ToString("O"))
            .Append(' ')
            .Append(level)
            .Append(' ')
            .Append(message);

        if (exception is not null)
        {
            builder.AppendLine().Append(exception);
        }

        builder.AppendLine();

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(CurrentLogPath, builder.ToString(), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
