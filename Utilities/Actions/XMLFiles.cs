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


        [Action("Replace XLIFF source with target", Description = "Swap <source> and <target> contents, exchange language attributes, and optionally remove target elements or set a new target language.")]
        public async Task<ConvertTextToDocumentResponse> ReplaceXliffSourceWithTarget([ActionParameter] ReplaceXliffRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            var doc = XDocument.Load(streamIn, LoadOptions.PreserveWhitespace);

            XNamespace ns = doc.Root.GetDefaultNamespace();

            var transUnits = doc.Descendants(ns + "trans-unit").ToList();
            foreach (var transUnit in transUnits)
            {
                var sourceElement = transUnit.Element(ns + "source");
                var targetElement = transUnit.Element(ns + "target");

                if (sourceElement == null || targetElement == null)
                    continue;

                var sourceNodes = sourceElement.Nodes().ToList();
                var targetNodes = targetElement.Nodes().ToList();

                sourceElement.RemoveNodes();
                targetElement.RemoveNodes();

                sourceElement.Add(targetNodes);
                targetElement.Add(sourceNodes);

                if (request.DeleteTargets == true)
                {
                    targetElement.Remove();
                }
            }

            XElement fileElement = doc.Root.Element(ns + "file");
            if (fileElement != null)
            {
                var sourceLangAttr = fileElement.Attribute("source-language");
                var targetLangAttr = fileElement.Attribute("target-language");

                if (sourceLangAttr != null && targetLangAttr != null)
                {
                    string originalSourceLang = sourceLangAttr.Value;
                    string originalTargetLang = targetLangAttr.Value;

                    if (!string.IsNullOrEmpty(request.SetNewSourceLanguage))
                    {
                        sourceLangAttr.Value = request.SetNewSourceLanguage;
                    }
                    else
                    {
                        sourceLangAttr.Value = originalTargetLang;
                    }

                    if (!string.IsNullOrEmpty(request.SetNewTargetLanguage))
                    {
                        targetLangAttr.Value = request.SetNewTargetLanguage;
                    }
                    else
                    {
                        targetLangAttr.Value = originalSourceLang;
                    }
                }
            }
            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                doc.Save(writer);
            }
            streamOut.Position = 0;

            var resultFile = await _fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);
            return new ConvertTextToDocumentResponse { File = resultFile };
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
