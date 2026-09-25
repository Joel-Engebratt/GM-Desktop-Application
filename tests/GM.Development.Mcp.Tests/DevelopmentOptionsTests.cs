namespace GM.Development.Mcp.Tests
{
    [TestClass]
    public sealed class DevelopmentOptionsTests
    {
        [TestMethod]
        public void ConfigurationRejectsPathTraversal() =>
            Assert.ThrowsExactly<ArgumentException>(() => DevelopmentOptions.Parse(
                ["--repo-root", TestPaths.RepositoryRoot, "--configuration", "../../other"]));

        [TestMethod]
        public void OptionRequiresValue() =>
            Assert.ThrowsExactly<ArgumentException>(() => DevelopmentOptions.Parse(["--repo-root"]));

        [TestMethod]
        public void UnknownOptionIsRejected() =>
            Assert.ThrowsExactly<ArgumentException>(() => DevelopmentOptions.Parse(["--execute", "anything"]));
    }
}
