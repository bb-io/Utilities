using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers;

public class FontStaticDataSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        return new Dictionary<string, string>
        {
            { "Arial", "Arial" },
            { "Calibri", "Calibri" },
            { "Cambria", "Cambria" },
            { "Courier New", "Courier New" },
            { "Georgia", "Georgia" },
            { "Times New Roman", "Times New Roman" },
            { "Verdana", "Verdana" },
            { "Tahoma", "Tahoma" },
            { "Trebuchet MS", "Trebuchet MS" },
            { "Arial Black", "Arial Black" },
            { "Impact", "Impact" },
            { "Comic Sans MS", "Comic Sans MS" },
            { "Franklin Gothic Medium", "Franklin Gothic Medium" },
            { "Garamond", "Garamond" },
            { "Palatino Linotype", "Palatino Linotype" },
            { "Book Antiqua", "Book Antiqua" },
            { "Lucida Sans Unicode", "Lucida Sans Unicode" },
            { "Segoe UI", "Segoe UI" },
            { "Century Gothic", "Century Gothic" },
            { "Gill Sans", "Gill Sans" },
            { "Helvetica", "Helvetica" },
            { "Lucida Console", "Lucida Console" },
            { "Futura", "Futura" },
            { "Optima", "Optima" },
            { "Candara", "Candara" },
            { "Geneva", "Geneva" },
            { "Baskerville", "Baskerville" },
            { "Avant Garde", "Avant Garde" },
            { "Perpetua", "Perpetua" },
            { "Rockwell", "Rockwell" },
            { "Goudy Old Style", "Goudy Old Style" },
            { "Bookman", "Bookman" },
            { "Century Schoolbook", "Century Schoolbook" },
            { "Bell MT", "Bell MT" },
            { "Copperplate Gothic", "Copperplate Gothic" },
            { "Brush Script MT", "Brush Script MT" }
        };
    }
}