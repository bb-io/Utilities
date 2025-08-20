namespace Apps.Utilities.Models.Files;
public class PageCountResponse
{
    public List<PageCountResult> Files { get; set; } = new List<PageCountResult>();
    public double TotalPages { get; set; }
}
public class PageCountResult
{
    public string FileName { get; set; }
    public double PageCount { get; set; }
}