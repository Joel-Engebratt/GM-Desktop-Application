using System.Diagnostics;
using ModelContextProtocol.Protocol;

namespace GM.Development.Mcp.Tests
{
    [TestClass]
    [TestCategory("DesktopMcp")]
    public sealed class DesktopMcpTests
    {
        private McpTestClient server = null!;
        private SessionInfo session = null!;

        [TestInitialize]
        public async Task LaunchIsolatedSession()
        {
            if (Environment.GetEnvironmentVariable("GM_RUN_MCP_UI_TESTS") != "1")
                Assert.Inconclusive("Opt-in desktop tests: run scripts/test-mcp-ui.ps1 in an interactive Windows session.");
            server = await McpTestClient.ConnectAsync();
            session = await server.ReadAsync<SessionInfo>("gm_launch");
            await server.FindAsync("Library.New");
        }

        [TestCleanup]
        public async Task CloseIsolatedSession()
        {
            if (server is not null) await server.DisposeAsync();
        }

        [TestMethod]
        public void LaunchUsesDisposableCampaignDirectory()
        {
            Assert.StartsWith(Path.Combine(TestPaths.RepositoryRoot, "artifacts", "mcp") + Path.DirectorySeparatorChar, session.DataDirectory);
        }

        [TestMethod]
        public async Task LaunchReusesActiveProcess()
        {
            var secondLaunch = await server.ReadAsync<SessionInfo>("gm_launch");

            Assert.AreEqual(session.ProcessId, secondLaunch.ProcessId);
        }

        [TestMethod]
        public async Task InspectionReportsTruncationAtNodeLimit()
        {
            var snapshot = await server.ReadAsync<UiSnapshot>("gm_inspect", new() { ["maxNodes"] = 1 });

            Assert.IsTrue(snapshot.Truncated);
            Assert.HasCount(1, snapshot.Nodes);
        }

        [TestMethod]
        public async Task DisabledOpenCannotBeInvoked()
        {
            var open = await server.FindAsync("Library.Open");

            var result = await server.CallAsync("gm_invoke", new() { ["elementId"] = open.Id });

            Assert.IsFalse(open.Enabled);
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task SystemChoiceExpansionControlsItemVisibility(bool expanded)
        {
            await server.InvokeAsync("Library.New");
            var choice = await server.FindAsync("Create.System");
            if (!expanded)
            {
                await server.CallSuccessfullyAsync("gm_expand", new() { ["elementId"] = choice.Id, ["expanded"] = true });
                choice = await server.FindAsync("Create.System");
            }

            await server.CallSuccessfullyAsync("gm_expand", new() { ["elementId"] = choice.Id, ["expanded"] = expanded });
            var snapshot = await server.InspectAsync();

            Assert.AreEqual(expanded, snapshot.Nodes.Any(node => node.ControlType == "ControlType.ListItem" && !node.Offscreen));
        }

        [TestMethod]
        public async Task NewInspectionInvalidatesOldElementReferences()
        {
            await server.InvokeAsync("Library.New");
            var name = await server.FindAsync("Create.Name");
            await server.InspectAsync();

            var result = await server.CallAsync("gm_set_text", new() { ["elementId"] = name.Id, ["text"] = "stale" });

            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Content.OfType<TextContentBlock>().Single().Text, "stale");
        }

        [TestMethod]
        public async Task BlankFormDoesNotCreateCampaignFile()
        {
            await server.InvokeAsync("Library.New");

            await server.InvokeAsync("Create.Save");
            await server.FindAsync("Create.Name");

            Assert.IsFalse(Directory.Exists(session.DataDirectory) &&
                Directory.EnumerateFiles(session.DataDirectory, "campaign.json", SearchOption.AllDirectories).Any());
        }

        [TestMethod]
        public async Task CreatingCampaignOpensItsSavedPage()
        {
            await server.CreateCampaignAsync();

            var heading = await server.FindAsync("Campaign.Name");

            Assert.AreEqual("MCP smoke campaign", heading.Name);
            Assert.HasCount(1, Directory.GetFiles(session.DataDirectory, "campaign.json", SearchOption.AllDirectories));
        }

        [TestMethod]
        public async Task MinimumSizeWindowProducesPngCapture()
        {
            await server.CreateCampaignAsync();
            await server.InvokeAsync("Campaign.Back");
            await server.CallSuccessfullyAsync("gm_resize", new() { ["width"] = 660, ["height"] = 480 });
            await server.FindAsync("Library.Open");

            var capture = await server.CallSuccessfullyAsync("gm_capture");

            var image = capture.Content.OfType<ImageContentBlock>().Single();
            Assert.IsGreaterThan(1000, image.DecodedData.Length);
            CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, image.DecodedData.Span[..8].ToArray());
            Console.WriteLine("MCP screenshot: " + capture.Content.OfType<TextContentBlock>().Single().Text);
        }

        [TestMethod]
        public async Task RestartRetainsCampaignForReopening()
        {
            await server.CreateCampaignAsync();

            var restarted = await server.ReadAsync<SessionInfo>("gm_restart");
            await server.FindAsync("Library.Campaigns");
            var row = (await server.InspectAsync()).Nodes.First(node => node.ControlType == "ControlType.DataItem");
            await server.CallSuccessfullyAsync("gm_select", new() { ["elementId"] = row.Id });
            await server.InvokeAsync("Library.Open");

            Assert.AreEqual(session.DataDirectory, restarted.DataDirectory);
            Assert.AreEqual("MCP smoke campaign", (await server.FindAsync("Campaign.Name")).Name);
        }

        [TestMethod]
        public async Task CloseTerminatesOwnedProcess()
        {
            using var process = Process.GetProcessById(session.ProcessId);

            await server.CallSuccessfullyAsync("gm_close");

            Assert.IsTrue(process.HasExited);
        }
    }
}
