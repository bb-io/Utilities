﻿using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using System.Xml.Linq;
using System.Xml;
using Apps.Utilities.Models.XMLFiles;

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
        [Action("Change XML file property", Description = "Change XML file property")]
        public async Task<ConvertTextToDocumentResponse> ChangeXML([ActionParameter] ChangeXMLRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);
            var doc = XDocument.Load(streamIn);
            var items = doc.Root.Descendants(request.Property);

            foreach (var itemElement in items)
                itemElement.Value = request.Value;

            await using var streamOut = new MemoryStream();
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            var writer = XmlWriter.Create(streamOut, settings);
            doc.Save(writer);
            await writer.FlushAsync();

            var resultFile =
                await _fileManagementClient.UploadAsync(streamOut, request.File.ContentType, request.File.Name);
            return new ConvertTextToDocumentResponse
            {
                File = resultFile
            };
        }

        [Action("Get XML file property", Description = "Get XML file property")]
        public async Task<GetXMLPropertyResponse> GetXMLProperty([ActionParameter] GetXMLPropertyRequest request)
        {
            await using var streamIn = await _fileManagementClient.DownloadAsync(request.File);

            var doc = XDocument.Load(streamIn);
            try 
            {
                var items = doc.Root.Descendants(request.Property);
                var text = String.IsNullOrEmpty(request.Attribute) ?
                items.First().Value :
                items.First().Attribute(request.Attribute)?.Value;
                return new() { Value = text };
            }
            catch 
            {
                try 
                {
                    var element = doc.Element(request.Property);
                    var text = String.IsNullOrEmpty(request.Attribute) ?
                    element?.Value :
                    element?.Attribute(request.Attribute)?.Value;
                    return new() { Value = text ?? ""};
                } 
                catch (Exception x)
                {
                    if (x.Message.ToLower().Contains("sequence contains no elements"))
                    {
                        throw new Exception("The specified property or attribute is not present in the file");
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
