using System.IO;
using System.Text.Json;
using GM_Desktop_Application.Models;

namespace GM_Desktop_Application.Services
{
    public sealed class JsonCampaignStore : ICampaignStore
    {
        public const int MaxMetadataBytes = 64 * 1024;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private readonly string root;
        private readonly TimeProvider clock;

        public JsonCampaignStore(string? storageRoot = null, TimeProvider? clock = null)
        {
            root = storageRoot ?? Path.Combine(AppContext.BaseDirectory, "Data", "Campaigns");
            this.clock = clock ?? TimeProvider.System;
        }

        public Task<CampaignLibrary> ListAsync() => Task.Run(async () =>
        {
            string[] directories;
            try
            {
                // Enumeration (unlike Directory.Exists) preserves permission and I/O errors.
                directories = Directory.GetDirectories(root);
            }
            catch (DirectoryNotFoundException)
            {
                return new CampaignLibrary([], []);
            }

            var campaigns = new List<Campaign>();
            var issues = new List<CampaignLoadIssue>();
            foreach (var directory in directories)
            {
                try
                {
                    await using var stream = new FileStream(Path.Combine(directory, "campaign.json"), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    // Check before allocating/parsing, then bound the read even if the file changes.
                    if (stream.Length > MaxMetadataBytes)
                    {
                        throw new InvalidDataException("Campaign metadata exceeds the 64 KiB limit.");
                    }
                    var bytes = new byte[MaxMetadataBytes + 1];
                    var count = 0;
                    while (count < bytes.Length)
                    {
                        var read = await stream.ReadAsync(bytes.AsMemory(count)).ConfigureAwait(false);
                        if (read == 0) break;
                        count += read;
                    }
                    if (count > MaxMetadataBytes)
                    {
                        throw new InvalidDataException("Campaign metadata exceeds the 64 KiB limit.");
                    }
                    using var boundedStream = new MemoryStream(bytes, 0, count, writable: false);
                    using var document = await JsonDocument.ParseAsync(boundedStream).ConfigureAwait(false);
                    var data = document.RootElement;
                    if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("formatVersion", out var version) || !version.TryGetInt32(out var number))
                    {
                        throw new InvalidDataException("Missing or invalid format version.");
                    }
                    if (number != 1)
                    {
                        throw new InvalidDataException($"Unsupported campaign format version {number}.");
                    }

                    var campaign = data.Deserialize<Campaign>(JsonOptions);
                    if (campaign is null || campaign.Id == Guid.Empty ||
                        !Guid.TryParse(Path.GetFileName(directory), out var folderId) || folderId != campaign.Id ||
                        !new CampaignDraft(campaign.Name, campaign.SystemId, campaign.CustomSystemName).IsValid ||
                        campaign.CreatedAt == default || campaign.UpdatedAt < campaign.CreatedAt ||
                        campaign.CreatedAt.Offset != TimeSpan.Zero || campaign.UpdatedAt.Offset != TimeSpan.Zero)
                    {
                        throw new InvalidDataException("Invalid campaign metadata or folder ID.");
                    }
                    campaigns.Add(campaign);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException)
                {
                    issues.Add(new CampaignLoadIssue(directory, exception.Message));
                }
            }
            return new CampaignLibrary(campaigns, issues);
        });

        public Task<Campaign> CreateAsync(CampaignDraft draft)
        {
            if (!draft.IsValid)
            {
                throw new ArgumentException("Campaign text or rules system is invalid.", nameof(draft));
            }

            return Task.Run(async () =>
            {
                var now = clock.GetUtcNow().ToUniversalTime();
                var campaign = new Campaign
                {
                    Id = Guid.NewGuid(), Name = draft.Name.Trim(), SystemId = draft.SystemId,
                    CustomSystemName = draft.CustomSystemName.Trim(), CreatedAt = now, UpdatedAt = now
                };
                var directory = Path.Combine(root, campaign.Id.ToString("D"));
                var temporaryPath = Path.Combine(directory, "campaign.json.tmp");
                var finalPath = Path.Combine(directory, "campaign.json");
                Directory.CreateDirectory(directory);
                try
                {
                    await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await JsonSerializer.SerializeAsync(stream, campaign, JsonOptions).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                        stream.Flush(flushToDisk: true);
                    }
                    // Same-directory rename publishes the complete document, without overwriting a campaign.
                    File.Move(temporaryPath, finalPath);
                    return campaign;
                }
                catch
                {
                    try
                    {
                        File.Delete(temporaryPath);
                        if (!Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory);
                        }
                    }
                    catch (Exception cleanupError) when (cleanupError is IOException or UnauthorizedAccessException)
                    {
                        // Preserve the original save failure; leftover files are reported on the next load.
                    }
                    throw;
                }
            });
        }
    }
}
