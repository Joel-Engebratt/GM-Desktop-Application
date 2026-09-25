using ModelContextProtocol.Protocol;

namespace GM.Development.Mcp.Tests
{
    [TestClass]
    public sealed class McpIntegrationTests
    {
        [TestMethod]
        public async Task StdioHandshakeDiscoversDevelopmentTools()
        {
            await using var server = await McpTestClient.ConnectAsync();

            var tools = await server.Client.ListToolsAsync(cancellationToken: server.Token);

            CollectionAssert.AreEquivalent(new[] { "gm_launch", "gm_inspect", "gm_invoke", "gm_set_text", "gm_select",
                "gm_expand", "gm_resize", "gm_capture", "gm_close", "gm_restart" }, tools.Select(tool => tool.Name).ToArray());
        }

        [TestMethod]
        public async Task InspectionWithoutSessionReturnsActionableToolError()
        {
            await using var server = await McpTestClient.ConnectAsync();

            var result = await server.CallAsync("gm_inspect");

            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Content.OfType<TextContentBlock>().Single().Text, "gm_launch");
        }

        [TestMethod]
        public async Task InvalidResizeReturnsToolError()
        {
            await using var server = await McpTestClient.ConnectAsync();

            var result = await server.CallAsync("gm_resize", new() { ["width"] = -1, ["height"] = 480 });

            Assert.IsTrue(result.IsError);
        }
    }
}
