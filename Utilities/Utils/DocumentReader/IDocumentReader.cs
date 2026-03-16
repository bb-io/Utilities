namespace Apps.Utilities.Utils.DocumentReader;

public interface IDocumentReader
{
    Task<string> Read(Stream file);
}
