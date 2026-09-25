using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;

namespace GM.Desktop.Tests.ViewModels
{
    internal static class CampaignTestData
    {
        public static Campaign Campaign(string name = "Name", string system = "Homebrew", int day = 24, int id = 1) => new()
        {
            Id = new Guid($"00000000-0000-0000-0000-{id:D12}"),
            Name = name,
            SystemId = "custom",
            CustomSystemName = system,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 9, day, 0, 0, 0, TimeSpan.Zero)
        };
    }

    internal sealed class FakeCampaignStore : ICampaignStore
    {
        public List<Campaign> Campaigns { get; } = [];
        public IReadOnlyList<CampaignLoadIssue> Issues { get; set; } = [];
        public Exception? LoadError { get; set; }
        public Exception? SaveError { get; set; }
        public TaskCompletionSource<CampaignLibrary>? PendingLoad { get; set; }
        public TaskCompletionSource<Campaign>? PendingSave { get; set; }
        public int Creates { get; private set; }

        public Task<CampaignLibrary> ListAsync() => LoadError is not null
            ? Task.FromException<CampaignLibrary>(LoadError)
            : PendingLoad?.Task ?? Task.FromResult(new CampaignLibrary(Campaigns.ToArray(), Issues));

        public Task<Campaign> CreateAsync(CampaignDraft draft)
        {
            Creates++;
            if (SaveError is not null) return Task.FromException<Campaign>(SaveError);
            if (PendingSave is not null) return PendingSave.Task;
            var campaign = CampaignTestData.Campaign(draft.Name.Trim(), draft.CustomSystemName.Trim(), id: Creates + 100);
            Campaigns.Add(campaign);
            return Task.FromResult(campaign);
        }
    }
}
