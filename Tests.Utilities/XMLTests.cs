using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Xml;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class XMLTests : TestBase
{
    private XMLFiles _xmlActions;
    private Files _fileActions;

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, true);
        Directory.CreateDirectory(outputDirectory);
        _xmlActions = new XMLFiles(FileManager);
        _fileActions = new Files(InvocationContext, FileManager, CreateLogger<Files>());
    }

    //Get XML propetry using XPath
    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_value()
    {
        var file = new FileReference { Name = "simple.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            Attribute = "version",
            File = file,
            XPath = "//meta"
        });
        Assert.AreEqual("1.0", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_value_with_namespaces()
    {
        var file = new FileReference { Name = "namespace.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            Attribute = "version",
            File = file,
            XPath = "//ns:meta",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("1.0", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_value()
    {
        var file = new FileReference { Name = "simple.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//title"
        });
        Assert.AreEqual("Foo", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_with_namespaces()
    {
        var file = new FileReference { Name = "namespace.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:title",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("Foo", response.Value);
        Console.WriteLine(response.Value);
    }

    //Get XML propetry using XDocument
    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_value()
    {
        var file = new FileReference { Name = "simple.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            Attribute = "version",
            File = file,
            Property = "meta"
        });
        Assert.AreEqual("1.0", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_value_with_namespaces()
    {
        var file = new FileReference { Name = "namespace.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            Attribute = "version",
            File = file,
            Property = "meta",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("1.0", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_value()
    {
        var file = new FileReference { Name = "simple.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            Property = "title"
        });
        Assert.AreEqual("Foo", response.Value);
        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_with_namespaces()
    {
        var file = new FileReference { Name = "namespace.xml" };
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            Property = "title",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("Foo", response.Value);
        Console.WriteLine(response.Value);
    }

    //Change XML property XPath
    [TestMethod]
    public async Task XPath_Change_XML_property_value_works()
    {
        var file = new FileReference { Name = "simple.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            XPath = "//title",
            Value = "Bar"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//title"
        });
        Assert.AreEqual("Bar", response.Value);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_attribute_works()
    {
        var file = new FileReference { Name = "simple.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            XPath = "//meta",
            Value = "2.0",
            Attribute = "version"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//meta",
            Attribute = "version"
        });
        Assert.AreEqual("2.0", response.Value);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_value_works_with_namespace()
    {
        var file = new FileReference { Name = "namespace.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            XPath = "//ns:title",
            Value = "Bar",
            Namespace = "http://example.com/ns"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:title",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("Bar", response.Value);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_attribute_works_with_namespace()
    {
        var file = new FileReference { Name = "namespace.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            XPath = "//ns:meta",
            Value = "2.0",
            Attribute = "version",
            Namespace = "http://example.com/ns"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:meta",
            Attribute = "version",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("2.0", response.Value);
    }

    //Change XML property xDocument
    [TestMethod]
    public async Task Change_XML_property_value_works()
    {
        var file = new FileReference { Name = "simple.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            Property = "title",
            Value = "Bar"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//title"
        });
        Assert.AreEqual("Bar", response.Value);
    }


    [TestMethod]
    public async Task Change_XML_property_attribute_works()
    {
        var file = new FileReference { Name = "simple.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            Property = "meta",
            Value = "2.0",
            Attribute = "version"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//meta",
            Attribute = "version"
        });
        Assert.AreEqual("2.0", response.Value);
    }

    [TestMethod]
    public async Task Change_XML_property_value_works_with_namespace()
    {
        var file = new FileReference { Name = "namespace.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            Property = "title",
            Value = "Bar",
            Namespace = "http://example.com/ns"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:title",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("Bar", response.Value);
    }

    [TestMethod]
    public async Task Change_XML_property_attribute_works_with_namespace()
    {
        var file = new FileReference { Name = "namespace.xml" };
        await _xmlActions.ChangeXML(new ChangeXMLRequest
        {
            File = file,
            Property = "meta",
            Value = "2.0",
            Attribute = "version",
            Namespace = "http://example.com/ns"
        });
        var response = await _xmlActions.GetXMLProperty(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:meta",
            Attribute = "version",
            Namespace = "http://example.com/ns"
        });
        Assert.AreEqual("2.0", response.Value);
    }

    [TestMethod]
    public async Task Count_Words_In_Html()
    {
        var actions = new Files(InvocationContext, FileManager, CreateLogger<Files>());
        var file = new FileReference { Name = "test.html" };
        var input = new FileDto { File = file };
        var result = await actions.GetWordCountInFile(input);
        Console.WriteLine(result);
        Assert.AreEqual(71, result);
    }

    [TestMethod]
    public async Task Concat_Strings()
    {
        var actions = new Texts(InvocationContext);
        var input = new ConcatenateStringsInput { Strings = new List<string>{ "Apple", "Banana", "Cherry" }, Delimiter="," };
        var result = actions.ConcatenateStrings(input);

        Console.WriteLine(result);
        Assert.AreEqual("Apple,Banana,Cherry", result);
    }

    [TestMethod]
    public async Task ReplaceXliffTargetWithSource_ReturnsSucces()
    {
        var actions = new Xliff(FileManager);
        var input = new ReplaceXliffRequest { File=new FileReference { Name= "test.xliff" }, /*DeleteTargets = true, SetNewTargetLanguage = "nl"*/ };
        var result = actions.ReplaceXliffSourceWithTarget(input);

        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task ChangeXliffSource_ReturnsSucces()
    {
        var actions = new Xliff(FileManager);
        var input = new ReplaceXliffRequest { File = new FileReference { Name = "test.xliff" }, SetNewSourceLanguage="nl" };
        var result = actions.ReplaceXliffSourceWithTarget(input);

        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public async Task Get_XML_Properties_Using_Property_Returns_All_Values()
    {
        var file = new FileReference { Name = "multiple_meta.xml" };
        var response = await _xmlActions.GetXMLProperties(new GetXMLPropertyRequest
        {
            File = file,
            Property = "meta",
            Attribute = "version"
        });

        var expectedValues = new List<string> { "1.0", "2.0", "3.0" };
        CollectionAssert.AreEqual(expectedValues.ToArray(), response.Values.ToArray());
        Console.WriteLine(string.Join(", ", response.Values));
    }
    [TestMethod]
    public async Task XPath_Get_XML_Properties_Returns_All_Values()
    {
        var file = new FileReference { Name = "multiple_meta.xml" };
        var response = await _xmlActions.GetXMLProperties(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//meta",
            Attribute = "version"
        });

        var expectedValues = new List<string> { "1.0", "2.0", "3.0" };
        CollectionAssert.AreEqual(expectedValues.ToArray(), response.Values.ToArray());
        Console.WriteLine(string.Join(", ", response.Values));
    }

    [TestMethod]
    public async Task XPath_Get_XML_Properties_With_Namespace_Returns_All_Values()
    {
        var file = new FileReference { Name = "multiple_namespace.xml" };
        var response = await _xmlActions.GetXMLProperties(new GetXMLPropertyRequest
        {
            File = file,
            XPath = "//ns:meta",
            Attribute = "version",
            Namespace = "http://example.com/ns"
        });

        var expectedValues = new List<string> { "1.0", "2.0" };
        CollectionAssert.AreEqual(expectedValues.ToArray(), response.Values.ToArray());
        Console.WriteLine(string.Join(", ", response.Values));
    }

    [TestMethod]
    public async Task Get_XML_Properties_Using_Property_Returns_All_Values_No_Attribute()
    {
        var file = new FileReference { Name = "multiple_titles.xml" };
        var response = await _xmlActions.GetXMLProperties(new GetXMLPropertyRequest
        {
            File = file,
            Property = "title"
        });

        var expectedValues = new List<string> { "Foo", "Bar" };
        CollectionAssert.AreEqual(expectedValues.ToArray(), response.Values.ToArray());
        Console.WriteLine(string.Join(", ", response.Values));
    }


    [TestMethod]
    public async Task ExtractContentAsHtml_ReturnsSucces()
    {
        var actions = new Scraping(InvocationContext,FileManager);
        var input = new LoadDocumentRequest { File = new FileReference { Name = "8CFA7618-C9EB-4AA5-B282-03B7A7CBA7AF.html" } };
        var string1 = string.Empty;
        var result = actions.ExtractHtmlContent(input, string1);

        Console.WriteLine(result.Result.Content);
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public async Task ExtractWebContentAsHtml_ReturnsSucces()
    {
        var actions = new Scraping(InvocationContext, FileManager);
        var input = "https://dev.blackbird.io/";
        var string1 = string.Empty;
        var result = actions.ExtractWebContent(input, string1);

        Console.WriteLine(result.Result.Content);
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public async Task ConvertHtmlToXliff_ReturnsSucces()
    {
        var actions = new Xliff(FileManager);
        var input = new ConvertHtmlToXliffRequest { File = new FileReference { Name = "Starting a flight.html" }, SourceLanguage = "en", TargetLanguage = "de" };
        var result = actions.ConvertHtmlToXliff(input);

        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task ConvertXliffToHtml_ReturnsSucces()
    {
        var actions = new Xliff(FileManager);
        var input = new ConvertXliffToHtmlRequest { File = new FileReference { Name = "Global HR Solutions & Employment Tools for Distributed Teams _ Remote.xliff" } };
        var result = actions.ConvertXliffToHtml(input);

        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public async Task ConvertMultilingualToBilingual_ReturnsSucces()
    {
        var actions = new XMLFiles(FileManager);
        var input = new ConvertTbxToBilingualRequest { File = new FileReference { Name = "Original.tbx" }, SourceLanguage = "de", TargetLanguage = "nl" };
        var result = actions.ConvertTbxToBilingual(input);

        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }

    private string GetTestFolderPath()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        return config["TestFolder"];
    }
}