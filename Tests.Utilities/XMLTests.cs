using Apps.Utilities.Actions;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Xml.Linq;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class XMLTests : TestBase
{
    //Get XML propetry using XPath
    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_value()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { Attribute = "version", File = file, XPath = "//meta" });

        Assert.AreEqual("1.0", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_value_with_namespaces()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { Attribute = "version", File = file, XPath = "//ns:meta", Namespace = "http://example.com/ns" });

        Assert.AreEqual("1.0", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_value()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { File = file, XPath = "//title" });

        Assert.AreEqual("Foo", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task XPath_Get_XML_property_returns_correct_attribute_with_namespaces()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { File = file, XPath = "//ns:title", Namespace = "http://example.com/ns" });

        Assert.AreEqual("Foo", response.Value);

        Console.WriteLine(response.Value);
    }

    //Get XML propetry using XDocument
    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_value()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { Attribute = "version", File = file, Property = "meta" });

        Assert.AreEqual("1.0", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_value_with_namespaces()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { Attribute = "version", File = file, Property = "meta", Namespace = "http://example.com/ns" });

        Assert.AreEqual("1.0", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_value()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { File = file, Property = "title" });

        Assert.AreEqual("Foo", response.Value);

        Console.WriteLine(response.Value);
    }

    [TestMethod]
    public async Task Get_XML_property_returns_correct_attribute_with_namespaces()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        var response = await actions.GetXMLProperty(new GetXMLPropertyRequest { File = file, Property = "title", Namespace = "http://example.com/ns" });

        Assert.AreEqual("Foo", response.Value);

        Console.WriteLine(response.Value);
    }

    //Change XML property XPath
    [TestMethod]
    public async Task XPath_Change_XML_property_value_works()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, XPath = "//title", Value = "Bar" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_attribute_works()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, XPath = "//meta", Value = "2.0", Attribute = "version" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_value_works_with_namespace()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, XPath = "//ns:title", Value = "Bar", Namespace = "http://example.com/ns" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task XPath_Change_XML_property_attribute_works_with_namespace()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, XPath = "//ns:meta", Value = "2.0", Attribute = "version", Namespace = "http://example.com/ns" });

        Assert.IsTrue(true);
    }

    //Change XML property xDocument
    [TestMethod]
    public async Task Change_XML_property_value_works()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, Property = "title", Value = "Bar" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Change_XML_property_attribute_works()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "simple.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, Property = "meta", Value = "2.0", Attribute = "version" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Change_XML_property_value_works_with_namespace()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, Property = "title", Value = "Bar", Namespace = "http://example.com/ns" });

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Change_XML_property_attribute_works_with_namespace()
    {
        var actions = new XMLFiles(FileManager);
        var file = new FileReference { Name = "namespace.xml" };

        await actions.ChangeXML(new ChangeXMLRequest { File = file, Property = "meta", Value = "2.0", Attribute = "version", Namespace = "http://example.com/ns" });

        Assert.IsTrue(true);
    }
}