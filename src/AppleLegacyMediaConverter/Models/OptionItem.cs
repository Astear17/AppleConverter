namespace AppleLegacyMediaConverter.Models;

public sealed record OptionItem<T>(string Label, T Value)
{
    public override string ToString() => Label;
}
