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

        [Action("Convert HTML to XLIFF", Description = "Convert HTML file to XLIFF 1.2 format")]
        public async Task<ConvertTextToDocumentResponse> ConvertHtmlToXliff([ActionParameter] ConvertHtmlToXliffRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            string htmlContent;
            using (var reader = new StreamReader(streamIn, Encoding.UTF8))
            {
                htmlContent = await reader.ReadToEndAsync();
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.OptionFixNestedTags = true;
            htmlDoc.LoadHtml(htmlContent);

            XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";

            var fileElement = new XElement(ns + "file",
                 new XAttribute("original", request.File.Name),
                 new XAttribute("source-language", string.IsNullOrEmpty(request.SourceLanguage) ? "en" : request.SourceLanguage),
                 new XAttribute("target-language", string.IsNullOrEmpty(request.TargetLanguage) ? "en" : request.TargetLanguage),
                 new XAttribute("datatype", "html")
            );

            fileElement.AddFirst(new XElement("originalFile", htmlContent));

            var bodyElement = new XElement(ns + "body");
            int transUnitId = 1;

            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerHtml))
            {
                string transformedTitle = TransformInlineTagsForXliff(titleNode.InnerHtml);
                var titleTransUnit = new XElement(ns + "trans-unit",
                    new XAttribute("id", transUnitId.ToString()),
                    new XAttribute("slug", "title"),
                    new XAttribute("tag", "title")
                );
                titleTransUnit.Add(new XElement(ns + "source", transformedTitle));
                titleTransUnit.Add(new XElement(ns + "target", ""));
                bodyElement.Add(titleTransUnit);
                transUnitId++;
            }

            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                foreach (var child in bodyNode.ChildNodes)
                {
                    if (child.NodeType == HtmlNodeType.Element && !string.IsNullOrWhiteSpace(child.InnerText))
                    {
                        string slugAttr = child.GetAttributeValue("slug", null) ?? child.GetAttributeValue("id", null);
                        if (string.IsNullOrEmpty(slugAttr))
                        {
                            slugAttr = "body_" + transUnitId;
                        }
                        string tagName = child.Name.ToLower();
                        string transformedContent = TransformInlineTagsForXliff(child.InnerHtml);
                        var transUnit = new XElement(ns + "trans-unit",
                            new XAttribute("id", transUnitId.ToString()),
                            new XAttribute("slug", slugAttr),
                            new XAttribute("tag", tagName)
                        );
                        transUnit.Add(new XElement(ns + "source", transformedContent));
                        transUnit.Add(new XElement(ns + "target", ""));
                        bodyElement.Add(transUnit);
                        transUnitId++;
                    }

                    if (child.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(child.InnerText))
                    {
                        string textContent = child.InnerText.Trim();
                        string slugAttr = "body_text_" + transUnitId;
                        string transformedContent = TransformInlineTagsForXliff(textContent);
                        var transUnit = new XElement(ns + "trans-unit",
                            new XAttribute("id", transUnitId.ToString()),
                            new XAttribute("slug", slugAttr),
                            new XAttribute("tag", "p")
                        );
                        transUnit.Add(new XElement(ns + "source", transformedContent));
                        transUnit.Add(new XElement(ns + "target", ""));
                        bodyElement.Add(transUnit);
                        transUnitId++;
                    }
                }
            }

            var xliffDoc = new XDocument(
                 new XElement(ns + "xliff",
                     new XAttribute("version", "1.2"),
                     fileElement,
                     bodyElement
                 )
            );

            using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (var writer = XmlWriter.Create(streamOut, settings))
            {
                xliffDoc.Save(writer);
            }
            streamOut.Position = 0;

            var fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".xliff";
            var resultFile = await _fileManagementClient.UploadAsync(streamOut, "application/xml", fileName);
            return new ConvertTextToDocumentResponse { File = resultFile };
        }

        [Action("Convert XLIFF to HTML", Description = "Convert XLIFF file (version 1.2) to HTML file")]
        public async Task<ConvertTextToDocumentResponse> ConvertXliffToHtml([ActionParameter] ConvertXliffToHtmlRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            var xliffDoc = XDocument.Load(streamIn, LoadOptions.PreserveWhitespace);
            XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";

            var fileElement = xliffDoc.Descendants(ns + "file").FirstOrDefault();
            string originalHtml = null;
            if (fileElement != null)
            {
                var originalFileElement = fileElement.Element("originalFile");
                if (originalFileElement != null)
                {
                    originalHtml = originalFileElement.Value;
                }
            }

            var htmlDoc = new HtmlDocument();
            if (!string.IsNullOrEmpty(originalHtml))
                htmlDoc.LoadHtml(originalHtml);
            else
                htmlDoc.LoadHtml("<html><head><title></title></head><body></body></html>");

            var transUnits = xliffDoc.Descendants(ns + "trans-unit").ToList();
            foreach (var transUnit in transUnits)
            {
                var targetElement = transUnit.Element(ns + "target");
                string translated = (targetElement != null && !string.IsNullOrWhiteSpace(targetElement.Value))
                                       ? targetElement.Value
                                       : transUnit.Element(ns + "source")?.Value ?? "";
                string revertedContent = RevertInlineTagsFromXliff(translated);

                var slug = transUnit.Attribute("slug")?.Value;
                var tag = transUnit.Attribute("tag")?.Value;
                if (!string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(tag))
                {
                    if (slug == "title" && tag == "title")
                    {
                        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
                        if (titleNode != null)
                            titleNode.InnerHtml = revertedContent;
                        else
                        {
                            var headNode = htmlDoc.DocumentNode.SelectSingleNode("//head");
                            if (headNode != null)
                            {
                                var newTitle = htmlDoc.CreateElement("title");
                                newTitle.InnerHtml = revertedContent;
                                headNode.AppendChild(newTitle);
                            }
                        }
                    }
                    else
                    {
                        var xpathQuery = $"//body//{tag}[@slug='{slug}' or @id='{slug}']";
                        var targetNode = htmlDoc.DocumentNode.SelectSingleNode(xpathQuery);
                        if (targetNode != null)
                        {
                            targetNode.InnerHtml = revertedContent;
                        }
                        else
                        {
                            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                            if (bodyNode != null)
                            {
                                var newElem = htmlDoc.CreateElement(tag);
                                newElem.SetAttributeValue("slug", slug);
                                newElem.InnerHtml = revertedContent;
                                bodyNode.AppendChild(newElem);
                            }
                        }
                    }
                }
            }

            string updatedHtml;
            using (var sw = new StringWriter())
            {
                htmlDoc.Save(sw);
                updatedHtml = sw.ToString();
            }

            using var streamOut = new MemoryStream();
            using (var writer = new StreamWriter(streamOut, Encoding.UTF8, 1024, true))
            {
                await writer.WriteAsync(updatedHtml);
                await writer.FlushAsync();
            }
            streamOut.Position = 0;

            var fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".html";
            var resultFile = await _fileManagementClient.UploadAsync(streamOut, "text/html", fileName);
            return new ConvertTextToDocumentResponse { File = resultFile };
        }
        private string TransformInlineTagsForXliff(string html)
        {
            XElement container;
            try
            {
                container = XElement.Parse("<container>" + html + "</container>");
            }
            catch
            {
                return html;
            }

            var counters = new Dictionary<string, int>();
            return TransformNodes(container.Nodes(), counters);
        }

        private string TransformNodes(IEnumerable<XNode> nodes, Dictionary<string, int> counters)
        {
            var sb = new StringBuilder();
            foreach (var node in nodes)
            {
                if (node is XText textNode)
                {
                    sb.Append(textNode.Value);
                }
                else if (node is XElement element)
                {
                    string tagName = element.Name.LocalName.ToLower();
                    if (tagName == "br")
                    {
                        int id = GetNextId(counters, tagName);
                        sb.Append($"<ph id=\"{id}\">&lt;br/&gt;</ph>");
                    }
                    else
                    {
                        int id = GetNextId(counters, tagName);
                        sb.Append($"<bpt id=\"{id}\">&lt;{tagName}&gt;</bpt>");
                        sb.Append(TransformNodes(element.Nodes(), counters));
                        sb.Append($"<ept id=\"{id}\">&lt;/{tagName}&gt;</ept>");
                    }
                }
            }
            return sb.ToString();
        }

        private int GetNextId(Dictionary<string, int> counters, string tag)
        {
            if (!counters.ContainsKey(tag))
                counters[tag] = 1;
            else
                counters[tag]++;
            return counters[tag];
        }

        private string RevertInlineTagsFromXliff(string inlineContent)
        {
            inlineContent = Regex.Replace(inlineContent, @"<bpt id=""\d+"">&lt;(\w+)&gt;</bpt>", "<$1>");
            inlineContent = Regex.Replace(inlineContent, @"<ept id=""\d+"">&lt;/(\w+)&gt;</ept>", "</$1>");
            inlineContent = Regex.Replace(inlineContent, @"<ph id=""\d+"">&lt;(\w+\/?)&gt;</ph>", "<$1>");
            return inlineContent;
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
