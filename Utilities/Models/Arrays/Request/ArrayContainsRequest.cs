namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayContainsRequest
{
    public IEnumerable<string> Array { get; set; }
    
    public string Entry { get; set; }
}