﻿using Apps.Utilities.Models.Files;
using Apps.Utilities.Models.XMLFiles;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using Blackbird.Applications.Sdk.Common.Files;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.Actions
{
    [ActionList("XLIFF")]
    public class Xliff
    {
        private readonly IFileManagementClient _fileManagementClient;

        public Xliff(IFileManagementClient fileManagementClient)
        {
            _fileManagementClient = fileManagementClient;
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


        [Action("Convert HTML to XLIFF", Description = "Convert HTML file to XLIFF 1.2 format")]
        public async Task<ConvertTextToDocumentResponse> ConvertHtmlToXliff([ActionParameter] ConvertHtmlToXliffRequest request)
        {
            string htmlContent = await DownloadHtmlContentAsync(request.File);
            htmlContent = RemoveInvalidXmlChars(htmlContent);

            HtmlDocument htmlDoc = ParseHtmlDocument(htmlContent);
            XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";

            XElement fileElement = CreateFileNode(request.File.Name, request.SourceLanguage, request.TargetLanguage, htmlContent, ns);
            XElement bodyElement = CreateBodyNodes(htmlDoc, ns);

            XDocument xliffDoc = new XDocument(
                 new XElement(ns + "xliff",
                     new XAttribute("version", "1.2"),
                     fileElement,
                     bodyElement
                 )
            );

            MemoryStream streamOut = WriteXmlToMemoryStream(xliffDoc);
            string fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".xliff";
            var resultFile = await _fileManagementClient.UploadAsync(streamOut, "application/xml", fileName);
            return new ConvertTextToDocumentResponse { File = resultFile };
        }

        [Action("Convert XLIFF to HTML", Description = "Convert XLIFF file (version 1.2) to HTML file")]
        public async Task<ConvertTextToDocumentResponse> ConvertXliffToHtml([ActionParameter] ConvertXliffToHtmlRequest request)
        {
            string ext = Path.GetExtension(request.File.Name)?.ToLowerInvariant();
            if (ext != ".xliff" && ext != ".xlf")
            {
                throw new PluginMisconfigurationException("Wrong format file: expected XLIFF (.xliff or .xlf), not " + ext);
            }

            try
            {

                XDocument xliffDoc = await LoadXliffDocumentAsync(request.File);
                XNamespace ns = "urn:oasis:names:tc:xliff:document:1.2";
                HtmlDocument htmlDoc = LoadOriginalHtmlDocument(xliffDoc, ns);

                ApplyTranslationsToHtml(htmlDoc, xliffDoc, ns);

                string updatedHtml = GetHtmlFromDocument(htmlDoc);
                MemoryStream streamOut = WriteTextToMemoryStream(updatedHtml);
                string fileName = Path.GetFileNameWithoutExtension(request.File.Name) + ".html";
                var resultFile = await _fileManagementClient.UploadAsync(streamOut, "text/html", fileName);
                return new ConvertTextToDocumentResponse { File = resultFile };

            }
            catch (XmlException ex)
            {
                throw new PluginMisconfigurationException($"Invalid XLIFF file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new PluginApplicationException("Error converting XLIFF to HTML", ex);
            }
        }

        private async Task<XDocument> LoadXliffDocumentAsync(FileReference file)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(file);
            return XDocument.Load(streamIn, LoadOptions.PreserveWhitespace);
        }

        private string RemoveInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", "");
        }

        private HtmlDocument LoadOriginalHtmlDocument(XDocument xliffDoc, XNamespace ns)
        {
            var fileElement = xliffDoc.Descendants(ns + "file").FirstOrDefault();
            string originalHtml = null;
            if (fileElement != null)
            {
                var originalFileElement = fileElement.Element(XName.Get("originalFile", ""));
                if (originalFileElement == null)
                {
                    originalFileElement = fileElement.Element(XName.Get("originalFile", "urn:oasis:names:tc:xliff:document:1.2"));
                }
                if (originalFileElement != null)
                {
                    originalHtml = originalFileElement.Value;
                }
            }

            var doc = new HtmlDocument();
            if (!string.IsNullOrEmpty(originalHtml))
            {
                doc.LoadHtml(originalHtml);
            }
            else
            {
                doc.LoadHtml("<html><head><title></title></head><body></body></html>");
            }
            return doc;
        }

        private void ApplyTranslationsToHtml(HtmlDocument htmlDoc, XDocument xliffDoc, XNamespace ns)
        {
            var transUnits = xliffDoc.Descendants(ns + "trans-unit")
                .OrderBy(t => {var raw = (string)t.Attribute("id");
                    var m = Regex.Match(raw, @"\d+$");
                    return m.Success ? int.Parse(m.Value) : int.MaxValue;}).ToList();

            var titleTransUnit = transUnits.FirstOrDefault(t =>
                                      string.Equals((string)t.Attribute("slug"), "title", StringComparison.OrdinalIgnoreCase) &&
                                      string.Equals((string)t.Attribute("tag"), "title", StringComparison.OrdinalIgnoreCase));
            if (titleTransUnit != null)
            {
                string titleTranslation = GetTranslatedText(titleTransUnit, ns);
                var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                if (titleNode != null)
                {
                    titleNode.InnerHtml = RevertInlineTagsFromXliff(titleTranslation);
                }
            }

            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                var translatableNodes = new List<HtmlNode>();
                foreach (var child in bodyNode.ChildNodes)
                {
                    if ((child.NodeType == HtmlNodeType.Element && !string.IsNullOrWhiteSpace(child.InnerText)) ||
                        (child.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(child.InnerText)))
                    {
                        translatableNodes.Add(child);
                    }
                }

                var bodyTransUnits = transUnits.Where(t => !string.Equals((string)t.Attribute("slug"), "title", StringComparison.OrdinalIgnoreCase))
                                                .ToList();

                int count = Math.Min(translatableNodes.Count, bodyTransUnits.Count);
                for (int i = 0; i < count; i++)
                {
                    string translation = GetTranslatedText(bodyTransUnits[i], ns);
                    translation = RevertInlineTagsFromXliff(translation);
                    translatableNodes[i].InnerHtml = translation;
                }
            }
        }
        private string GetHtmlFromDocument(HtmlDocument doc)
        {
            using (var sw = new StringWriter())
            {
                doc.Save(sw);
                return sw.ToString();
            }
        }
        private MemoryStream WriteTextToMemoryStream(string text)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.Write(text);
                writer.Flush();
            }
            stream.Position = 0;
            return stream;
        }
        private string GetTranslatedText(XElement transUnit, XNamespace ns)
        {
            var targetElement = transUnit.Element(ns + "target");
            if (targetElement != null && !string.IsNullOrWhiteSpace(targetElement.Value))
                return string.Concat(targetElement.Nodes());
            else
                return string.Concat(transUnit.Element(ns + "source")?.Nodes() ?? Enumerable.Empty<XNode>());
        }
        private IEnumerable<XNode> TransformInlineTagsForXliffNodes(string html)
        {
            XElement container;
            try
            {
                container = XElement.Parse("<container>" + html + "</container>");
            }
            catch
            {
                return new List<XNode> { new XText(html) };
            }
            var counters = new Dictionary<string, int>();
            return TransformNodesToNodes(container.Nodes(), counters);
        }
        private IEnumerable<XNode> TransformNodesToNodes(IEnumerable<XNode> nodes, Dictionary<string, int> counters)
        {
            foreach (var node in nodes)
            {
                if (node is XText textNode)
                {
                    yield return new XText(textNode.Value);
                }
                else if (node is XElement element)
                {
                    string tagName = element.Name.LocalName.ToLower();
                    int id = GetNextId(counters, tagName);

                    if (IsVoidInlineElement(tagName))
                    {
                        string attributes = string.Join("", element.Attributes().Select(a => $" {a.Name}=\"{a.Value}\""));
                        string selfClosingTag = $"<{tagName}{attributes}/>";
                        string encodedTag = HttpUtility.HtmlEncode(selfClosingTag);
                        yield return new XElement("ph", new XAttribute("id", id), encodedTag);
                    }
                    else
                    {
                        string attributes = string.Join("", element.Attributes().Select(a => $" {a.Name}=\"{a.Value}\""));
                        string openTag = HttpUtility.HtmlEncode($"<{tagName}{attributes}>");
                        string closeTag = HttpUtility.HtmlEncode($"</{tagName}>");

                        yield return new XElement("bpt", new XAttribute("id", id), openTag);
                        foreach (var child in TransformNodesToNodes(element.Nodes(), counters))
                        {
                            yield return child;
                        }
                        yield return new XElement("ept", new XAttribute("id", id), closeTag);
                    }
                }
            }
        }
        private bool IsVoidInlineElement(string tagName)
        {
            var voidElements = new HashSet<string> { "br", "hr", "img", "input", "link", "base", "wbr", "area", "embed", "col", "source", "track" };
            return voidElements.Contains(tagName);
        }
        private async Task<string> DownloadHtmlContentAsync(FileReference file)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(file);
            using (var reader = new StreamReader(streamIn, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
        private HtmlDocument ParseHtmlDocument(string htmlContent)
        {
            var doc = new HtmlDocument();
            doc.OptionFixNestedTags = true;
            doc.LoadHtml(htmlContent);
            return doc;
        }
        private XElement CreateFileNode(string fileName, string sourceLanguage, string targetLanguage, string htmlContent, XNamespace ns)
        {
            var fileElement = new XElement(ns + "file",
                 new XAttribute("original", fileName),
                 new XAttribute("source-language", string.IsNullOrEmpty(sourceLanguage) ? "en" : sourceLanguage),
                 new XAttribute("target-language", string.IsNullOrEmpty(targetLanguage) ? "en" : targetLanguage),
                 new XAttribute("datatype", "html")
            );
            fileElement.AddFirst(new XElement("originalFile", htmlContent));
            return fileElement;
        }
        private XElement CreateBodyNodes(HtmlDocument htmlDoc, XNamespace ns)
        {
            var bodyElement = new XElement(ns + "body");
            int transUnitId = 1;

            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerHtml))
            {
                var transformedNodes = TransformInlineTagsForXliffNodes(titleNode.InnerHtml).ToList();
                var titleTransUnit = new XElement(ns + "trans-unit",
                    new XAttribute("id", transUnitId.ToString()),
                    new XAttribute("slug", "title"),
                    new XAttribute("tag", "title")
                );
                titleTransUnit.Add(new XElement(ns + "source", transformedNodes));
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
                        string slugAttr = child.GetAttributeValue("slug", null)
                            ?? child.GetAttributeValue("id", null);
                        if (string.IsNullOrEmpty(slugAttr))
                        {
                            slugAttr = "body_" + transUnitId;
                        }
                        string tagName = child.Name.ToLower();
                        var transformedNodes = TransformInlineTagsForXliffNodes(child.InnerHtml).ToList();
                        var transUnit = new XElement(ns + "trans-unit",
                            new XAttribute("id", transUnitId.ToString()),
                            new XAttribute("slug", slugAttr),
                            new XAttribute("tag", tagName)
                        );
                        transUnit.Add(new XElement(ns + "source", transformedNodes));
                        transUnit.Add(new XElement(ns + "target", ""));
                        bodyElement.Add(transUnit);
                        transUnitId++;
                        child.SetAttributeValue("data-slug", slugAttr);
                    }
                    else if (child.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(child.InnerText))
                    {
                        string textContent = child.InnerText.Trim();
                        string slugAttr = "body_text_" + transUnitId;
                        var transformedNodes = TransformInlineTagsForXliffNodes(textContent).ToList();
                        var transUnit = new XElement(ns + "trans-unit",
                            new XAttribute("id", transUnitId.ToString()),
                            new XAttribute("slug", slugAttr),
                            new XAttribute("tag", "p")
                        );
                        transUnit.Add(new XElement(ns + "source", transformedNodes));
                        transUnit.Add(new XElement(ns + "target", ""));
                        bodyElement.Add(transUnit);
                        transUnitId++;
                    }
                }
            }
            return bodyElement;
        }
        private MemoryStream WriteXmlToMemoryStream(XDocument doc)
        {
            var stream = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = true,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (var writer = XmlWriter.Create(stream, settings))
            {
                doc.Save(writer);
            }
            stream.Position = 0;
            return stream;
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
            string decodedContent = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(inlineContent));
            decodedContent = Regex.Replace(decodedContent, @"<bpt[^>]*>(.*?)</bpt>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            decodedContent = Regex.Replace(decodedContent, @"<ept[^>]*>(.*?)</ept>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            decodedContent = Regex.Replace(decodedContent, @"<ph[^>]*>(.*?)</ph>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return decodedContent;
        }
    }
}
