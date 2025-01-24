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

        [Action("Bump version string", Description = "Bump version string")]
        public async Task<GetXMLPropertyResponse> BumpVersionString([ActionParameter] BumpVersionStringRequest request)
        {
            Version version = Version.Parse(request.VersionString);
            int major = request.VersionType == "major" ? version.Major + 1 : version.Major;
            int minor = request.VersionType == "minor" ? version.Minor + 1 : version.Minor;
            int patch = request.VersionType == "patch" ? version.Build + 1 : version.Build;
            return new GetXMLPropertyResponse() { Value = $"{major}.{minor}.{patch}" };
        }


        [Action("Change XML file property value", Description = "Change XML file property value or attribute value")]
        public async Task<ConvertTextToDocumentResponse> ChangeXML([ActionParameter] ChangeXMLRequest request)
        {
            if (string.IsNullOrEmpty(request.Property) && string.IsNullOrEmpty(request.XPath)
                 || !string.IsNullOrEmpty(request.Property) && !string.IsNullOrEmpty(request.XPath))
            {
                throw new PluginMisconfigurationException("You must fill exactly one of [Property] or [XPath].");
            }

            if (!string.IsNullOrEmpty(request.Property))
            {
                if (request.Property.Contains(':'))
                {
                    throw new PluginMisconfigurationException("You cannot use the ':' character in property names. Use the [Namespace] or [XPath] approach instead!");
                }
                if (request.Property.StartsWith("/"))
                {
                    throw new PluginMisconfigurationException("The property must not start with a '/' character. Ensure that your property is correctly formatted as a name.");
                }
                return await ChangeXmlUsingProperty(request);
            }
            else
            {
                return await ChangeXmlUsingXPath(request);
            }
        }

        [Action("Get XML file property value", Description = "Get XML file property value or attribute value")]
        public async Task<GetXMLPropertyResponse> GetXMLProperty([ActionParameter] GetXMLPropertyRequest request)
        {
            if (string.IsNullOrEmpty(request.Property) && string.IsNullOrEmpty(request.XPath)
            || !string.IsNullOrEmpty(request.Property) && !string.IsNullOrEmpty(request.XPath))
            {
                throw new PluginMisconfigurationException("You must fill exactly one of [Property] or [XPath].");
            }

            if (!string.IsNullOrEmpty(request.Property))
            {
                if (request.Property.Contains(':'))
                {
                    throw new PluginMisconfigurationException("You cannot use the ':' character in property names. Use the [Namespace] or [XPath] approach instead!");
                }
                if (request.Property.StartsWith("/"))
                {
                    throw new PluginMisconfigurationException("The property must not start with a '/' character. Ensure that your property is correctly formatted as a name.");
                }
                return await GetXmlPropertyUsingProperty(request);
            }
            else
            {
                return await GetXmlPropertyUsingXPath(request);
            }
        }

        private async Task<ConvertTextToDocumentResponse> ChangeXmlUsingProperty(ChangeXMLRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            XNamespace ns = request.Namespace ?? string.Empty;
            var doc = XDocument.Load(streamIn);

            var items = doc.Root.Descendants(ns + request.Property);

            if (!items.Any())
            {
                throw new PluginMisconfigurationException($"Element '{request.Property}' not found in XML.");
            }

            foreach (var itemElement in items)
            {
                if (!string.IsNullOrEmpty(request.Attribute)
                    && itemElement.Attribute(ns + request.Attribute) != null)
                {
                    var attribute = itemElement.Attribute(ns + request.Attribute);
                    if (attribute != null)
                    {
                        attribute.Value = request.Value;
                    }
                }
                else
                {
                    itemElement.Value = request.Value;
                }
            }

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }
            streamOut.Position = 0;

            var resultFile = await _fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);

            return new ConvertTextToDocumentResponse
            {
                File = resultFile
            };
        }
        private async Task<ConvertTextToDocumentResponse> ChangeXmlUsingXPath(ChangeXMLRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(streamIn);

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            if (!string.IsNullOrEmpty(request.Namespace))
            {
                nsManager.AddNamespace("ns", request.Namespace);
            }

            var nodes = xmlDoc.SelectNodes(request.XPath, nsManager);

            if (nodes == null || nodes.Count == 0)
                throw new PluginMisconfigurationException("No elements found for the specified XPath.");

            foreach (XmlNode node in nodes)
            {
                if (!string.IsNullOrEmpty(request.Attribute))
                {
                    var attribute = node.Attributes?[request.Attribute];
                    if (attribute == null)
                    {
                        throw new PluginMisconfigurationException(
                            $"Attribute '{request.Attribute}' not found for node '{request.XPath}'.");
                    }
                    attribute.Value = request.Value;
                }
                else
                {
                    node.InnerText = request.Value;
                }
            }

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                xmlDoc.Save(writer);
            }
            streamOut.Position = 0;

            var updatedFile = await _fileManagementClient.UploadAsync(
                streamOut,
                request.File.ContentType,
                request.File.Name);

            return new ConvertTextToDocumentResponse
            {
                File = updatedFile
            };
        }


        private async Task<GetXMLPropertyResponse> GetXmlPropertyUsingXPath(GetXMLPropertyRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(streamIn);

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            if (!string.IsNullOrEmpty(request.Namespace))
            {
                nsManager.AddNamespace("ns", request.Namespace);
            }

            try
            {
                var nodes = xmlDoc.SelectNodes(request.XPath, nsManager);

                if (nodes == null || nodes.Count == 0)
                    throw new PluginMisconfigurationException("No elements found for the specified XPath.");

                var node = nodes[0];

                if (!string.IsNullOrEmpty(request.Attribute))
                {
                    var attribute = node.Attributes?[request.Attribute];
                    if (attribute == null)
                        throw new PluginMisconfigurationException($"Attribute '{request.Attribute}' not found on the node '{request.XPath}'.");

                    return new GetXMLPropertyResponse { Value = attribute.Value };
                }
                else
                {
                    return new GetXMLPropertyResponse { Value = node.InnerText };
                }
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException(ex.Message);
            }
        }
        private async Task<GetXMLPropertyResponse> GetXmlPropertyUsingProperty(GetXMLPropertyRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            XNamespace ns = request.Namespace ?? string.Empty;

            var doc = XDocument.Load(streamIn);
            try
            {
                var items = doc.Root.Descendants(ns + request.Property);
                if (items.Any())
                {
                    var text = string.IsNullOrEmpty(request.Attribute)
                        ? items.First().Value
                        : items.First().Attribute(request.Attribute)?.Value;

                    return new GetXMLPropertyResponse { Value = text };
                }
                else
                {
                    var element = doc.Element(ns + request.Property);
                    var text = string.IsNullOrEmpty(request.Attribute)
                        ? element?.Value
                        : element?.Attribute(ns + request.Attribute)?.Value;

                    if (text == null)
                        throw new PluginMisconfigurationException("The specified property or attribute is not present in the file");

                    return new GetXMLPropertyResponse { Value = text };
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("sequence contains no elements"))
                {
                    throw new PluginMisconfigurationException("The specified property or attribute is not present in the file");
                }
                throw new PluginApplicationException(ex.Message);
            }
        }

    }
}
