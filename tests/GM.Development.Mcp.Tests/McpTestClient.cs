using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace GM.Development.Mcp.Tests
{
    internal sealed class McpTestClient(McpClient client, CancellationTokenSource timeout) : IAsyncDisposable
    {
        public McpClient Client { get; } = client;
        public CancellationToken Token => timeout.Token;

        public static async Task<McpTestClient> ConnectAsync()
        {
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try
            {
                var client = await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
                {
                    Name = "GM development test",
                    Command = ".\\GM.Development.Mcp.exe",
                    WorkingDirectory = Path.GetDirectoryName(TestPaths.Server),
                    Arguments = ["--repo-root", TestPaths.RepositoryRoot, "--configuration", TestPaths.Configuration]
                }), cancellationToken: timeout.Token);
                return new McpTestClient(client, timeout);
            }
            catch { timeout.Dispose(); throw; }
        }

        public async Task<CallToolResult> CallAsync(string name, Dictionary<string, object?>? args = null) =>
            await Client.CallToolAsync(name, args, cancellationToken: Token);

        public async Task<CallToolResult> CallSuccessfullyAsync(string name, Dictionary<string, object?>? args = null)
        {
            var result = await CallAsync(name, args);
            Assert.AreNotEqual(true, result.IsError,
                $"{name}: " + string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text)));
            return result;
        }

        public async Task<T> ReadAsync<T>(string name, Dictionary<string, object?>? args = null)
        {
            var result = await CallSuccessfullyAsync(name, args);
            return JsonSerializer.Deserialize<T>(result.Content.OfType<TextContentBlock>().Single().Text)!;
        }

        public Task<UiSnapshot> InspectAsync() => ReadAsync<UiSnapshot>("gm_inspect");

        public async Task<UiNode> FindAsync(string automationId)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(10))
            {
                var snapshot = await InspectAsync();
                var node = snapshot.Nodes.FirstOrDefault(node => node.AutomationId == automationId && !node.Offscreen);
                if (node is not null) return node;
                await Task.Delay(100, Token);
            }
            throw new AssertFailedException($"Control did not appear: {automationId}");
        }

        public async Task InvokeAsync(string automationId)
        {
            var node = await FindAsync(automationId);
            await CallSuccessfullyAsync("gm_invoke", new() { ["elementId"] = node.Id });
        }

        public async Task SetTextAsync(string automationId, string text)
        {
            var node = await FindAsync(automationId);
            await CallSuccessfullyAsync("gm_set_text", new() { ["elementId"] = node.Id, ["text"] = text });
        }

        public async Task CreateCampaignAsync()
        {
            await InvokeAsync("Library.New");
            await SetTextAsync("Create.Name", "MCP smoke campaign");
            await SetTextAsync("Create.SystemName", "MCP test system");
            await InvokeAsync("Create.Save");
            await FindAsync("Campaign.Name");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var result = await Client.CallToolAsync("gm_close", cancellationToken: cleanup.Token);
                Assert.AreNotEqual(true, result.IsError, "Could not close the disposable MCP app.");
            }
            finally
            {
                try { await Client.DisposeAsync(); }
                finally { timeout.Dispose(); }
            }
        }
    }
}
