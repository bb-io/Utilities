using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class FontStaticDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new DataSourceItem("Arial", "Arial"),
            new DataSourceItem("Calibri", "Calibri"),
            new DataSourceItem("Cambria", "Cambria"),
            new DataSourceItem("Courier New", "Courier New"),
            new DataSourceItem("Georgia", "Georgia"),
            new DataSourceItem("Times New Roman", "Times New Roman"),
            new DataSourceItem("Verdana", "Verdana"),
            new DataSourceItem("Tahoma", "Tahoma"),
            new DataSourceItem("Trebuchet MS", "Trebuchet MS"),
            new DataSourceItem("Arial Black", "Arial Black"),
            new DataSourceItem("Impact", "Impact"),
            new DataSourceItem("Comic Sans MS", "Comic Sans MS"),
            new DataSourceItem("Franklin Gothic Medium", "Franklin Gothic Medium"),
            new DataSourceItem("Garamond", "Garamond"),
            new DataSourceItem("Palatino Linotype", "Palatino Linotype"),
            new DataSourceItem("Book Antiqua", "Book Antiqua"),
            new DataSourceItem("Lucida Sans Unicode", "Lucida Sans Unicode"),
            new DataSourceItem("Segoe UI", "Segoe UI"),
            new DataSourceItem("Century Gothic", "Century Gothic"),
            new DataSourceItem("Gill Sans", "Gill Sans"),
            new DataSourceItem("Helvetica", "Helvetica"),
            new DataSourceItem("Lucida Console", "Lucida Console"),
            new DataSourceItem("Futura", "Futura"),
            new DataSourceItem("Optima", "Optima"),
            new DataSourceItem("Candara", "Candara"),
            new DataSourceItem("Geneva", "Geneva"),
            new DataSourceItem("Baskerville", "Baskerville"),
            new DataSourceItem("Avant Garde", "Avant Garde"),
            new DataSourceItem("Perpetua", "Perpetua"),
            new DataSourceItem("Rockwell", "Rockwell"),
            new DataSourceItem("Goudy Old Style", "Goudy Old Style"),
            new DataSourceItem("Bookman", "Bookman"),
            new DataSourceItem("Century Schoolbook", "Century Schoolbook"),
            new DataSourceItem("Bell MT", "Bell MT"),
            new DataSourceItem("Copperplate Gothic", "Copperplate Gothic"),
            new DataSourceItem("Brush Script MT", "Brush Script MT")
        };
    }
}