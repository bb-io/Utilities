using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Xml.Linq;
using System.Xml;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.Actions
{
    [ActionList]
    public class XMLFiles
    {
        private readonly IFileManagementClient _fileManagementClient;

        public XMLFiles(IFileManagementClient fileManagementClient)
        {
            _fileManagementClient = fileManagementClient;
        }
        [Action("Change XML file property value", Description = "Change XML file property value or attribute value")]
        public async Task<ConvertTextToDocumentResponse> ChangeXML([ActionParameter] ChangeXMLRequest request)
        {
            if (request.Property.Contains(':'))
            {
                throw new PluginMisconfigurationException("You cannot use the ':' character in property names. Use the namespace optional input instead!");
            }
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);

            XNamespace ns = request.Namespace ?? string.Empty;
            var doc = XDocument.Load(streamIn);
            var items = doc.Root.Descendants(ns + request.Property);

            foreach (var itemElement in items)
            {
                if (request.Attribute != null && itemElement.Attribute(ns + request.Attribute) != null)
                {
                    var attribute = itemElement.Attribute(ns + request.Attribute);
                    if (attribute != null)
                    {
                        attribute.Value = request.Value;
                    }
                } else
                {
                    itemElement.Value = request.Value;
                }
            }                

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            var writer = XmlWriter.Create(streamOut, settings);
            doc.Save(writer);
            writer.Flush();
            streamOut.Position = 0;

            var resultFile =
                await _fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);
            return new ConvertTextToDocumentResponse
            {
                File = resultFile
            };
        }

        [Action("Get XML file property value", Description = "Get XML file property value or attribute value")]
        public async Task<GetXMLPropertyResponse> GetXMLProperty([ActionParameter] GetXMLPropertyRequest request)
        {
            if (request.Property.Contains(':'))
            {
                throw new PluginMisconfigurationException("You cannot use the ':' character in property names. Use the namespace optional input instead!");
            }
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            XNamespace ns = request.Namespace ?? string.Empty;

            var doc = XDocument.Load(streamIn);
            try 
            {
                var items = doc.Root.Descendants(ns + request.Property);
                var text = String.IsNullOrEmpty(request.Attribute) ?
                items.First().Value :
                items.First().Attribute(request.Attribute)?.Value;
                return new() { Value = text };
            }
            catch 
            {
                try 
                {
                    var element = doc.Element(ns + request.Property);
                    var text = String.IsNullOrEmpty(request.Attribute) ?
                    element?.Value :
                    element?.Attribute(request.Attribute)?.Value;
                    return new() { Value = text ?? ""};
                } 
                catch (Exception x)
                {
                    if (x.Message.ToLower().Contains("sequence contains no elements"))
                    {
                        throw new PluginMisconfigurationException("The specified property or attribute is not present in the file");
                    }
                    throw x;
                }
            }
        }

        [Action("Bump version string", Description = "Bump version string")]
        public async Task<GetXMLPropertyResponse> BumpVersionString([ActionParameter] BumpVersionStringRequest request)
        {
            Version version = Version.Parse(request.VersionString);
            int major = request.VersionType == "major" ? version.Major + 1 : version.Major;
            int minor = request.VersionType == "minor" ? version.Minor + 1 : version.Minor;
            int patch = request.VersionType == "patch" ? version.Build + 1 : version.Build;
            return new GetXMLPropertyResponse() { Value = $"{major}.{minor}.{patch}" };
        }

    }
}
