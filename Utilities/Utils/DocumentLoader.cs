using System.Xml;
using System.Xml.Linq;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.Utilities.Utils;

public static class DocumentLoader
{
    public static async Task<XDocument> LoadXDocument(
        FileReference file, 
        IFileManagementClient fileManagementClient,
        LoadOptions loadOptions = LoadOptions.None,
        string fileExtension = "XML")
    {
        await using var stream = await fileManagementClient.DownloadAsync(file);
        try
        {
            return XDocument.Load(stream, loadOptions);
        }
        catch (XmlException ex)
        {
            throw new PluginMisconfigurationException(
                $"Failed to parse the file as {fileExtension}. " +
                $"Ensure it is a valid {fileExtension} document. " +
                $"Details: {ex.Message}");
        }
    }

    public static async Task<XmlDocument> LoadXmlDocument(
        FileReference file,
        IFileManagementClient fileManagementClient)
    {
        await using var stream = await fileManagementClient.DownloadAsync(file);
        try
        {
            var doc = new XmlDocument();
            doc.Load(stream);
            return doc;
        }
        catch (XmlException ex)
        {
            throw new PluginMisconfigurationException(
                $"Failed to parse the file as XML. Ensure it is a valid XML document. Details: {ex.Message}");
        }
    }
}