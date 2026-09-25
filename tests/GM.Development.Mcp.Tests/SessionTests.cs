namespace GM.Development.Mcp.Tests
{
    [TestClass]
    public sealed class SessionTests
    {
        private DesktopSession session = null!;

        [TestInitialize]
        public void Initialize() => session = new(new DevelopmentOptions(TestPaths.RepositoryRoot, "Release"));

        [TestCleanup]
        public void Cleanup() => session.Dispose();

        [TestMethod]
        [DataRow(0)]
        [DataRow(501)]
        public void InspectRejectsOutOfRangeNodeLimit(int limit) =>
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Inspect(maxNodes: limit));

        [TestMethod]
        [DataRow(0)]
        [DataRow(21)]
        public void InspectRejectsOutOfRangeDepthLimit(int limit) =>
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Inspect(maxDepth: limit));

        [TestMethod]
        public void SetTextRejectsOversizedInput() =>
            Assert.ThrowsExactly<ArgumentException>(() => session.SetText("unknown", new string('x', 4097)));

        [TestMethod]
        [DataRow(659, 480)]
        [DataRow(2561, 480)]
        [DataRow(660, 479)]
        [DataRow(660, 1601)]
        public void ResizeRejectsOutOfRangeDimensions(int width, int height) =>
            Assert.ThrowsExactly<ArgumentException>(() => session.Resize(width, height));

        [TestMethod]
        public void InvokeRequiresRunningSession() =>
            Assert.ThrowsExactly<InvalidOperationException>(() => session.Invoke("unknown"));

        [TestMethod]
        public void RestartRequiresPreviousLaunch() =>
            Assert.ThrowsExactly<InvalidOperationException>(() => session.Restart());
    }
}
