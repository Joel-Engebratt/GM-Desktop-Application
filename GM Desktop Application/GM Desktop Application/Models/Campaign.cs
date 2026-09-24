using System.Globalization;

namespace GM_Desktop_Application.Models
{
    public sealed record Campaign
    {
        public int FormatVersion { get; init; } = 1;
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public string SystemId { get; init; } = "";
        public string CustomSystemName { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string LastUpdatedDisplay => UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.CurrentCulture);
    }

    public sealed record RulesSystem(string Id, string Name)
    {
        public static IReadOnlyList<RulesSystem> Supported { get; } = [new("custom", "Other / custom")];
    }

    public sealed record CampaignDraft(string Name, string SystemId, string CustomSystemName)
    {
        public string? NameError => CampaignText.Validate(Name, CampaignText.NameMaxLength, "campaign name");
        public string? SystemError => RulesSystem.Supported.All(system => system.Id != SystemId) ? "Choose a rules system." : null;
        public string? CustomSystemError => SystemId == "custom" ? CampaignText.Validate(CustomSystemName, CampaignText.SystemNameMaxLength, "system name") : null;
        public bool IsValid => NameError is null && SystemError is null && CustomSystemError is null;
    }
}
