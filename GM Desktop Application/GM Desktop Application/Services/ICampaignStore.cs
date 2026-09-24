using GM_Desktop_Application.Models;

namespace GM_Desktop_Application.Services
{
    public sealed record CampaignLoadIssue(string Folder, string Message);
    public sealed record CampaignLibrary(IReadOnlyList<Campaign> Campaigns, IReadOnlyList<CampaignLoadIssue> Issues);

    public interface ICampaignStore
    {
        Task<CampaignLibrary> ListAsync();
        Task<Campaign> CreateAsync(CampaignDraft draft);
    }
}
