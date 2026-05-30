namespace AppleLegacyMediaConverter.Core.Models;

public sealed class MediaConversionException : Exception
{
    public MediaConversionException(string userMessage, string technicalDetails, Exception? innerException = null)
        : base(userMessage, innerException)
    {
        UserMessage = userMessage;
        TechnicalDetails = technicalDetails;
    }

    public string UserMessage { get; }

    public string TechnicalDetails { get; }
}
