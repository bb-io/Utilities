using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Xml.Linq;
using System.Xml;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Web;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Actions
{
    [ActionList("XML files")]
    public class XMLFiles
    {
        private readonly IFileManagementClient _fileManagementClient;

        public XMLFiles(IFileManagementClient fileManagementClient)
        {
            _fileManagementClient = fileManagementClient;
        }

        [Action("Reduce multilingual glossary to bilingual", Description = "Convert a multilingual TBX file to bilingual by keeping only the specified language pair")]
        public async Task<ConvertTextToDocumentResponse> ConvertTbxToBilingual(
            [ActionParameter] ConvertTbxToBilingualRequest request)
        {
            if (request.File == null)
            {
                throw new PluginMisconfigurationException("TBX file must be provided.");
            }
            if (string.IsNullOrEmpty(request.SourceLanguage) || string.IsNullOrEmpty(request.TargetLanguage))
            {
                throw new PluginMisconfigurationException("Both languages to keep must be specified.");
            }

            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            XDocument doc;
            try
            {
                doc = XDocument.Load(streamIn);
            }
            catch (XmlException ex)
            {
                throw new PluginMisconfigurationException(
                    $"File «{request.File.Name}» is not valid TBX/XML: {ex.Message}");
            }
            var tbxNs = doc.Root?.GetDefaultNamespace() ?? throw new PluginMisconfigurationException("TBX file is missing a valid namespace.");
            var xmlNs = XNamespace.Xml;

            var conceptEntries = doc.Descendants(tbxNs + "conceptEntry").ToList();

            foreach (var entry in conceptEntries)
            {
                var langSecs = entry.Elements(tbxNs + "langSec").ToList();

                foreach (var langSec in langSecs)
                {
                    var lang = langSec.Attribute(xmlNs + "lang")?.Value;
                    if (string.IsNullOrEmpty(lang) || (lang != request.SourceLanguage && lang != request.TargetLanguage))
                    {
                        langSec.Remove();
                    }
                }

                var remainingLangs = entry.Elements(tbxNs + "langSec")
                    .Select(ls => ls.Attribute(xmlNs + "lang")?.Value)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToList();
                if (remainingLangs.Count < 2 || !remainingLangs.Contains(request.SourceLanguage) || !remainingLangs.Contains(request.TargetLanguage))
                {
                    entry.Remove();
                }
            }

            conceptEntries.Where(ce => !ce.HasElements).Remove();

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }
            streamOut.Position = 0;

            var resultFile = await _fileManagementClient.UploadAsync(streamOut, request.File.ContentType ?? "application/xml", request.File.Name);

            return new ConvertTextToDocumentResponse
            {
                File = resultFile
            };
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


        [Action("Get XML file property values", Description = "Get XML file property or attribute values and return all matching results")]
        public async Task<GetXMLPropertiesResponse> GetXMLProperties([ActionParameter] GetXMLPropertyRequest request)
        {
            if ((string.IsNullOrEmpty(request.Property) && string.IsNullOrEmpty(request.XPath))
                || (!string.IsNullOrEmpty(request.Property) && !string.IsNullOrEmpty(request.XPath)))
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
                return await GetXmlPropertiesUsingProperty(request);
            }
            else
            {
                return await GetXmlPropertiesUsingXPath(request);
            }
        }

      

        private async Task<GetXMLPropertiesResponse> GetXmlPropertiesUsingProperty(GetXMLPropertyRequest request)
        {
            try
            {
                await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
                XNamespace ns = request.Namespace ?? string.Empty;
                var doc = XDocument.Load(streamIn);

                var values = new List<string>();

                var items = doc.Root.Descendants(ns + request.Property);
                if (items.Any())
                {
                    foreach (var item in items)
                    {
                        string value = string.IsNullOrEmpty(request.Attribute)
                            ? item.Value
                            : item.Attribute(request.Attribute)?.Value;
                        if (value != null)
                        {
                            values.Add(value);
                        }
                    }
                }
                else
                {
                    var element = doc.Element(ns + request.Property);
                    string value = string.IsNullOrEmpty(request.Attribute)
                            ? element?.Value
                            : element?.Attribute(ns + request.Attribute)?.Value;
                    if (value != null)
                    {
                        values.Add(value);
                    }
                }

                if (!values.Any())
                    throw new PluginMisconfigurationException("The specified property or attribute is not present in the file");

                return new GetXMLPropertiesResponse { Values = values };
            }
            catch (System.Xml.XmlException ex)
            {
               
                throw new PluginApplicationException($"XML error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Unexpected error: {ex.Message}");
            }
        }
        private async Task<GetXMLPropertiesResponse> GetXmlPropertiesUsingXPath(GetXMLPropertyRequest request)
        {
            try
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

                var values = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    if (!string.IsNullOrEmpty(request.Attribute))
                    {
                        var attribute = node.Attributes?[request.Attribute];
                        if (attribute == null)
                            throw new PluginMisconfigurationException($"Attribute '{request.Attribute}' not found for node '{request.XPath}'.");
                        values.Add(attribute.Value);
                    }
                    else
                    {
                        values.Add(node.InnerText);
                    }
                }

                return new GetXMLPropertiesResponse { Values = values };
            }
            catch (System.Xml.XmlException ex)
            {
                throw new PluginApplicationException($"XML error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Unexpected error: {ex.Message}");
            }
        }

        private async Task<ConvertTextToDocumentResponse> ChangeXmlUsingProperty(ChangeXMLRequest request)
        {
            try
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
                    if (!string.IsNullOrEmpty(request.Attribute))
                    {
                        var attributeWithNs = itemElement.Attribute(ns + request.Attribute);

                        if (attributeWithNs == null)
                        {
                            attributeWithNs = itemElement.Attribute(request.Attribute);
                        }
                        if (attributeWithNs != null)
                        {
                            attributeWithNs.Value = request.Value;
                        }
                        else
                        {
                            throw new PluginMisconfigurationException($"Attribute '{request.Attribute}' not found in element '{request.Property}'.");
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
            catch (System.Xml.XmlException ex)
            {
                throw new PluginMisconfigurationException($"XML error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Unexpected error: {ex.Message}");
            }
        }
        private async Task<ConvertTextToDocumentResponse> ChangeXmlUsingXPath(ChangeXMLRequest request)
        {
            try
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
            catch (System.Xml.XmlException ex)
            {
                throw new PluginMisconfigurationException($"XML error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException($"Unexpected error: {ex.Message}");
            }
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
