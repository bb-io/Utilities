namespace Apps.Utilities.Models.XMLFiles;

public class XliffSegmentDto(string id, string source, string target)
{
    public string Id { get; } = id;
    public string Source { get; } = source;
    public string Target { get; } = target;
    public int CharacterCount => Source.Length + Target.Length;
}