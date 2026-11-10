using System.Text;
using System.Xml.Linq;
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Transformations;

namespace Tests.Utilities;

[TestClass]
public class PseudolocalizeFilesTests
{
    [DataTestMethod]
    [DataRow("hello.txt", "text/plain", "Hello 123", "[Ĥéľľó 999]")]
    [DataRow("hello.html", "text/html", "<html><body><p>Hello 123</p></body></html>", "[Ĥéľľó 999]")]
    public async Task NativeFiles_ProduceReloadableXliff22WithSkeleton(string name, string contentType, string text, string expected)
    {
        var client = new TrackingFileClient((name, text));
        var result = await Actions(client).PseudolocalizeFiles(Request(name, contentType));
        var file = result.Files.Single();
        Assert.AreEqual(name + ".xlf", file.Name);
        Assert.AreEqual("application/xliff+xml", file.ContentType);
        Assert.AreEqual("2.2", client.Xml(file).Root!.Attribute("version")!.Value);
        var transformation = client.Load(file);
        Assert.AreEqual("en", transformation.SourceLanguage);
        Assert.AreEqual(expected, string.Concat(transformation.GetUnits().Single().Segments.Single().Target.Select(part => part.Value)));
        Assert.IsFalse(string.IsNullOrWhiteSpace(transformation.Original));
    }

    [DataTestMethod]
    [DataRow("1.2")]
    [DataRow("2.0")]
    [DataRow("2.1")]
    [DataRow("2.2")]
    public async Task XliffVersions_ConvertTo22PreservingLocalesAndFilename(string version)
    {
        var text = version == "1.2"
            ? "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:1.2\" version=\"1.2\"><file original=\"sample\" source-language=\"en\" target-language=\"fr\" datatype=\"plaintext\"><body><trans-unit id=\"u1\"><source>Hello 12</source></trans-unit></body></file></xliff>"
            : Xliff(Unit("u1", "Hello 12"), version);
        var client = new TrackingFileClient(("sample.XLIFF", text));
        var file = (await Actions(client).PseudolocalizeFiles(Request("sample.XLIFF"))).Files.Single();
        Assert.AreEqual("sample.XLIFF", file.Name);
        var xml = client.Xml(file);
        Assert.AreEqual("2.2", xml.Root!.Attribute("version")!.Value);
        Assert.AreEqual("en", xml.Root.Attribute("srcLang")!.Value);
        Assert.AreEqual("fr", xml.Root.Attribute("trgLang")!.Value);
        Assert.AreEqual("[Ĥéľľó 99]", Target(xml, "u1").Value);
        Assert.AreEqual("Hello 12", Source(xml, "u1").Value);
        client.Load(file);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DefaultStates_OnlyChangesInitialOrMissingStateAndPreservesStates(bool emptySelection)
    {
        var body = Unit("missing", "Ab 12", target: "Old")
            + Unit("initial", "Ab 12", "initial", "Old")
            + Unit("translated", "Ab 12", "translated", "Translated")
            + Unit("reviewed", "Ab 12", "reviewed", "Reviewed")
            + Unit("final", "Ab 12", "final", "Final");
        var client = new TrackingFileClient(("sample.xlf", Xliff(body)));
        var request = Request("sample.xlf");
        request.StatesToPseudolocalize = emptySelection ? [] : null;
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(request)).Files.Single());
        Assert.AreEqual("[Áƀ 99]", Target(xml, "missing").Value);
        Assert.AreEqual("[Áƀ 99]", Target(xml, "initial").Value);
        foreach (var state in new[] { "translated", "reviewed", "final" })
        {
            Assert.AreEqual(char.ToUpperInvariant(state[0]) + state[1..], Target(xml, state).Value);
            Assert.AreEqual(state, Segment(xml, state).Attribute("state")!.Value);
        }
        Assert.AreEqual("initial", Segment(xml, "initial").Attribute("state")!.Value);
        Assert.AreEqual("initial", Segment(xml, "missing").Attribute("state")?.Value ?? "initial");
    }

    [TestMethod]
    public async Task ExplicitStates_UsesSourceAndKeepsReviewedState()
    {
        var client = new TrackingFileClient(("sample.xlf", Xliff(
            Unit("u1", "Hello 42", "reviewed", "Different target 123") + Unit("u2", "Skip", "initial", "Keep"))));
        var request = Request("sample.xlf");
        request.StatesToPseudolocalize = [" reviewed "];
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(request)).Files.Single());
        Assert.AreEqual("[Ĥéľľó 99]", Target(xml, "u1").Value);
        Assert.AreEqual("reviewed", Segment(xml, "u1").Attribute("state")!.Value);
        Assert.AreEqual("Keep", Target(xml, "u2").Value);
    }

    [TestMethod]
    public async Task PreservesInlineCodesAnnotationsMetadataAndNontranslatableContent()
    {
        const string source = "Hi <ph id=\"p1\" equiv=\"{123}\"/>there <pc id=\"p2\" type=\"fmt\">Bold 42</pc> and <mrk id=\"m1\" translate=\"no\" type=\"generic\">Locked 12</mrk> End";
        var body = "<unit id=\"u1\"><notes><note>Context 123</note></notes><segment id=\"s1\" state=\"initial\"><source xml:space=\"preserve\">" + source + "</source></segment></unit>"
            + "<group id=\"g1\" translate=\"no\">" + Unit("locked", "Locked 12", target: "Unchanged 12") + "</group>"
            + "<unit id=\"lockedUnit\" translate=\"no\"><segment><source>Locked 12</source><target>Keep 12</target></segment></unit>"
            + "<unit id=\"ignorable\"><ignorable id=\"i1\"><source>Ignore 12</source><target>Keep 12</target></ignorable></unit>"
            + Unit("whitespace", "   ", target: "Keep 12");
        var client = new TrackingFileClient(("sample.xlf", Xliff(body)));
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(Request("sample.xlf"))).Files.Single());
        var target = Target(xml, "u1");
        Assert.AreEqual("[Ĥí ţĥéŕé Ƀóľď 99 áńď Locked 12 Éńď]", target.Value);
        Assert.AreEqual("Hi there Bold 42 and Locked 12 End", Source(xml, "u1").Value);
        var ph = target.Descendants().Single(element => element.Name.LocalName == "ph");
        Assert.AreEqual("p1", ph.Attribute("id")!.Value);
        Assert.AreEqual("{123}", ph.Attribute("equiv")!.Value);
        Assert.AreEqual("p2", target.Descendants().Single(element => element.Name.LocalName == "pc").Attribute("id")!.Value);
        var annotation = target.Descendants().Single(element => element.Name.LocalName == "mrk");
        Assert.AreEqual("Locked 12", annotation.Value);
        Assert.AreEqual("no", annotation.Attribute("translate")!.Value);
        Assert.AreEqual("Context 123", xml.Descendants().Single(element => element.Name.LocalName == "note").Value);
        Assert.AreEqual("Unchanged 12", Target(xml, "locked").Value);
        Assert.AreEqual("Keep 12", Target(xml, "lockedUnit").Value);
        Assert.AreEqual("Keep 12", Target(xml, "ignorable").Value);
        Assert.AreEqual("Keep 12", Target(xml, "whitespace").Value);
    }

    [TestMethod]
    public async Task ProtectsPlaceholdersAndNestedExpressionsWhileTransformingDigits()
    {
        const string source = "Hello 0123456789 ١٢ 𝟙 {name} {0} {{name}} ${name} %s %1$d %+08.2f %2$*3$.*4$f %% {n, plural, one {One 1} other {Many 2}}";
        var client = new TrackingFileClient(("sample.xlf", Xliff(Unit("u1", source))));
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(Request("sample.xlf"))).Files.Single());
        Assert.AreEqual("[Ĥéľľó 9999999999 99 9 {name} {0} {{name}} ${name} %s %1$d %+08.2f %2$*3$.*4$f %% {n, plural, one {One 1} other {Many 2}}]", Target(xml, "u1").Value);
        Assert.AreEqual(source, Source(xml, "u1").Value);
    }

    [DataTestMethod]
    [DataRow(0, "[Áƀ😀中 99 {0}]")]
    [DataRow(1, "[Áƀ😀中 99 {0}~]")]
    [DataRow(30, "[Áƀ😀中 99 {0}~~~]")]
    [DataRow(100, "[Áƀ😀中 99 {0}~~~~~~~~]")]
    public async Task Expansion_CountsTextElementsExcludingPlaceholders(int expansion, string expected)
    {
        var client = new TrackingFileClient(("sample.xlf", Xliff(Unit("u1", "Ab😀中 12 {0}"))));
        var request = Request("sample.xlf");
        request.ExpansionPercentage = expansion;
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(request)).Files.Single());
        Assert.AreEqual(expected, Target(xml, "u1").Value);
    }

    [TestMethod]
    public async Task Expansion_ExcludesCodesAndNontranslatableSpans()
    {
        var client = new TrackingFileClient(("sample.xlf", Xliff(Unit("u1", "Ab<ph id=\"p1\"/><mrk id=\"m1\" translate=\"no\" type=\"generic\">Long locked text 123</mrk>"))));
        var request = Request("sample.xlf");
        request.ExpansionPercentage = 100;
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(request)).Files.Single());
        Assert.AreEqual("[ÁƀLong locked text 123~~]", Target(xml, "u1").Value);
    }

    [TestMethod]
    public async Task AnnotationSpanAcrossSegments_IsProtectedEvenWhenFirstSegmentIsNotSelected()
    {
        const string body = "<unit id=\"u1\">"
            + "<segment id=\"s1\" state=\"final\"><source>Before <sm id=\"a1\" translate=\"no\" type=\"generic\"/>Locked 12</source><target>Keep 12</target></segment>"
            + "<segment id=\"s2\" state=\"initial\"><source>Locked 34<em startRef=\"a1\"/> After 56</source></segment></unit>";
        var client = new TrackingFileClient(("sample.xlf", Xliff(body)));
        var file = (await Actions(client).PseudolocalizeFiles(Request("sample.xlf"))).Files.Single();
        var segments = client.Xml(file).Descendants().Where(element => element.Name.LocalName == "segment").ToArray();
        Assert.AreEqual("Keep 12", segments[0].Elements().Single(element => element.Name.LocalName == "target").Value);
        Assert.AreEqual("[Locked 34 Áƒţéŕ 99]", segments[1].Elements().Single(element => element.Name.LocalName == "target").Value);
        client.Load(file);
    }

    [TestMethod]
    public async Task FullyNontranslatableAnnotation_KeepsExistingTarget()
    {
        var client = new TrackingFileClient(("sample.xlf", Xliff(Unit("u1", "<mrk id=\"a1\" translate=\"no\" type=\"generic\">Locked 12</mrk>", target: "Keep 12"))));
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(Request("sample.xlf"))).Files.Single());
        Assert.AreEqual("Keep 12", Target(xml, "u1").Value);
    }

    [TestMethod]
    public async Task NestedFilesAndGroups_AreProcessedAndRespectTranslateNo()
    {
        var text = "<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"2.0\" srcLang=\"en\" trgLang=\"fr\">"
            + "<file id=\"f1\"><group id=\"g1\">" + Unit("u1", "Ab 12") + "</group></file>"
            + "<file id=\"f2\" translate=\"no\">" + Unit("u2", "Cd 34", target: "Keep 34") + "</file></xliff>";
        var client = new TrackingFileClient(("sample.xlf", text));
        var xml = client.Xml((await Actions(client).PseudolocalizeFiles(Request("sample.xlf"))).Files.Single());
        Assert.AreEqual("[Áƀ 99]", Target(xml, "u1").Value);
        Assert.AreEqual("Keep 34", Target(xml, "u2").Value);
    }

    [TestMethod]
    public async Task Files_AreUploadedAndDisposedBeforeNextDownload_InInputOrder()
    {
        var client = new TrackingFileClient(("first.txt", "One 1"), ("second.txt", "Two 2")) { NonSeekable = true };
        var request = new PseudolocalizeFilesRequest { Files = [Reference("first.txt", "text/plain"), Reference("second.txt", "text/plain")] };
        var result = await Actions(client).PseudolocalizeFiles(request);
        CollectionAssert.AreEqual(new[] { "first.txt.xlf", "second.txt.xlf" }, result.Files.Select(file => file.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "download:first.txt", "upload:first.txt.xlf", "dispose:first.txt", "download:second.txt", "upload:second.txt.xlf", "dispose:second.txt" }, client.Events);
        Assert.IsFalse(client.LastUpload!.CanRead);
    }

    [DataTestMethod]
    [DataRow("parse")]
    [DataRow("download")]
    [DataRow("upload")]
    public async Task Failure_IdentifiesFileAndStopsLaterDownloads(string failure)
    {
        var client = new TrackingFileClient(("first.txt", "One"), ("bad.xlf", "<xliff>"), ("last.txt", "Last"));
        if (failure == "download") client.FailDownload = "bad.xlf";
        if (failure == "upload")
        {
            client.Inputs["bad.xlf"] = Xliff(Unit("u1", "Hello"));
            client.FailUpload = "bad.xlf";
        }
        var request = new PseudolocalizeFilesRequest { Files = [Reference("first.txt", "text/plain"), Reference("bad.xlf"), Reference("last.txt", "text/plain")] };
        Exception exception = failure == "parse"
            ? await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => Actions(client).PseudolocalizeFiles(request))
            : await Assert.ThrowsExceptionAsync<PluginApplicationException>(() => Actions(client).PseudolocalizeFiles(request));
        StringAssert.Contains(exception.Message, "bad.xlf");
        Assert.IsTrue(client.Outputs.ContainsKey("first.txt.xlf"));
        Assert.IsFalse(client.Events.Contains("download:last.txt"));
        Assert.IsFalse(client.Outputs.ContainsKey("bad.xlf"));
        Assert.IsFalse(client.LastInput!.CanRead);
        Assert.IsFalse(client.LastUpload!.CanRead);
    }

    [DataTestMethod]
    [DataRow("empty")]
    [DataRow("null")]
    [DataRow("null-item")]
    [DataRow("unnamed")]
    [DataRow("states")]
    [DataRow("expansion")]
    public async Task InvalidInputs_FailBeforeDownloading(string scenario)
    {
        var client = new TrackingFileClient(("hello.txt", "Hello"));
        var request = Request("hello.txt", "text/plain");
        switch (scenario)
        {
            case "empty": request.Files = []; break;
            case "null": request.Files = null!; break;
            case "null-item": request.Files = [Reference("hello.txt"), null!]; break;
            case "unnamed": request.Files = [Reference("hello.txt"), new FileReference()]; break;
            case "states": request.StatesToPseudolocalize = ["unknown"]; break;
            case "expansion": request.ExpansionPercentage = -1; break;
        }
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => Actions(client).PseudolocalizeFiles(request));
        Assert.AreEqual(0, client.Events.Count);
    }

    private static Files Actions(TrackingFileClient client) => new(new InvocationContext(), client);
    private static FileReference Reference(string name, string type = "application/xliff+xml") => new() { Name = name, ContentType = type };
    private static PseudolocalizeFilesRequest Request(string name, string type = "application/xliff+xml") => new() { Files = [Reference(name, type)] };
    private static string Xliff(string body, string version = "2.0") => $"<xliff xmlns=\"urn:oasis:names:tc:xliff:document:2.0\" version=\"{version}\" srcLang=\"en\" trgLang=\"fr\"><file id=\"f1\">{body}</file></xliff>";
    private static string Unit(string id, string source, string? state = null, string? target = null) =>
        $"<unit id=\"{id}\"><segment id=\"s-{id}\"{(state is null ? "" : $" state=\"{state}\"")}><source xml:space=\"preserve\">{source}</source>{(target is null ? "" : $"<target>{target}</target>")}</segment></unit>";
    private static XElement Segment(XDocument xml, string unitId) => xml.Descendants().Single(element => element.Name.LocalName == "unit" && element.Attribute("id")?.Value == unitId).Elements().Single(element => element.Name.LocalName is "segment" or "ignorable");
    private static XElement Target(XDocument xml, string unitId) => Segment(xml, unitId).Elements().Single(element => element.Name.LocalName == "target");
    private static XElement Source(XDocument xml, string unitId) => Segment(xml, unitId).Elements().Single(element => element.Name.LocalName == "source");

    private sealed class TrackingFileClient(params (string Name, string Content)[] inputs) : IFileManagementClient
    {
        public Dictionary<string, string> Inputs { get; } = inputs.ToDictionary(input => input.Name, input => input.Content);
        public Dictionary<string, byte[]> Outputs { get; } = [];
        public List<string> Events { get; } = [];
        public Stream? LastInput { get; private set; }
        public Stream? LastUpload { get; private set; }
        public string? FailDownload { get; set; }
        public string? FailUpload { get; set; }
        public bool NonSeekable { get; set; }

        public async Task<Stream> DownloadAsync(FileReference reference)
        {
            if (LastInput is not null) Assert.IsFalse(LastInput.CanRead, "Previous input is still open.");
            if (LastUpload is not null) Assert.IsFalse(LastUpload.CanRead, "Previous output is still open.");
            Events.Add("download:" + reference.Name);
            await Task.Yield();
            if (reference.Name == FailDownload) throw new IOException("Download failed.");
            LastInput = new TrackingStream(Encoding.UTF8.GetBytes(Inputs[reference.Name]), () => Events.Add("dispose:" + reference.Name), NonSeekable);
            return LastInput;
        }

        public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
        {
            Assert.IsTrue(LastInput!.CanRead);
            LastUpload = stream;
            Events.Add("upload:" + fileName);
            await Task.Yield();
            if (fileName == FailUpload) throw new IOException("Upload failed.");
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            Outputs.Add(fileName, buffer.ToArray());
            return Reference(fileName, contentType);
        }

        public XDocument Xml(FileReference file) => XDocument.Parse(Encoding.UTF8.GetString(Outputs[file.Name]));
        public Transformation Load(FileReference file)
        {
            using var stream = new MemoryStream(Outputs[file.Name]);
            var result = Transformation.Load(stream, file.Name, file.ContentType);
            Assert.IsTrue(result.Success, result.Error);
            return result.Value;
        }
    }

    private sealed class TrackingStream(byte[] bytes, Action onDispose, bool nonSeekable) : MemoryStream(bytes)
    {
        private bool disposed;
        public override bool CanSeek => !nonSeekable && base.CanSeek;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                onDispose();
            }
            base.Dispose(disposing);
        }
    }
}
