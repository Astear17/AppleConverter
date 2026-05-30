using AppleLegacyMediaConverter.Core.Models;

namespace AppleLegacyMediaConverter.Models;

public sealed class ApplicationState
{
    public AppSettings Settings { get; set; } = new();

    public BackendStatus? BackendStatus { get; set; }
}
