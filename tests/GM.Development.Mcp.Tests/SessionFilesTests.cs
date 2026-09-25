namespace GM.Development.Mcp.Tests
{
    [TestClass]
    public sealed class SessionFilesTests
    {
        private string root = null!;
        private string Source => Path.Combine(root, "source");
        private string Destination => Path.Combine(root, "session");

        [TestInitialize]
        public void Initialize()
        {
            root = Path.Combine(Path.GetTempPath(), "GM-Mcp-Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Source);
        }

        [TestCleanup]
        public void Cleanup()
        {
            var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GM-Mcp-Tests")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsafe test path.");
            Directory.Delete(root, recursive: true);
        }

        [TestMethod]
        public void CopyBuildPreservesBuildFileContents()
        {
            File.WriteAllText(Path.Combine(Source, "app.dll"), "build");

            SessionFiles.CopyBuild(Source, Destination);

            Assert.AreEqual("build", File.ReadAllText(Path.Combine(Destination, "app.dll")));
        }

        [TestMethod]
        [DataRow("Data")]
        [DataRow("nested/data")]
        public void CopyBuildExcludesDataWithoutChangingSource(string relativeDirectory)
        {
            var data = Path.Combine(Source, relativeDirectory);
            Directory.CreateDirectory(data);
            File.WriteAllText(Path.Combine(data, "campaign.json"), "private data");

            SessionFiles.CopyBuild(Source, Destination);

            Assert.IsFalse(Directory.Exists(Path.Combine(Destination, relativeDirectory)));
            Assert.AreEqual("private data", File.ReadAllText(Path.Combine(data, "campaign.json")));
        }
    }
}
