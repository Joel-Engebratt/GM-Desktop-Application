using System.Text.Json;
using System.Text.Json.Nodes;
using GM_Desktop_Application.Models;
using GM_Desktop_Application.Services;

namespace GM.Desktop.Tests.Services
{
    [TestClass]
    public sealed class CampaignStoreTests
    {
        private string testRoot = null!;
        private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 30, 0, TimeSpan.Zero);
        private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
        private string CampaignRoot => Path.Combine(testRoot, "Data", "Campaigns");
        private JsonCampaignStore Store => new(CampaignRoot, new FixedClock());

        [TestInitialize]
        public void Initialize()
        {
            testRoot = Path.Combine(Path.GetTempPath(), "GM-Campaign-Tests", Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(testRoot);
        }

        [TestCleanup]
        public void Cleanup()
        {
            var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GM-Campaign-Tests")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(testRoot).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe test path.");
            Directory.Delete(testRoot, true);
        }

        [TestMethod]
        [DataRow("name-length")]
        [DataRow("system-length")]
        [DataRow("direction")]
        [DataRow("control")]
        [DataRow("surrogate")]
        public async Task UnsafeOrOversizedTextCannotBypassValidationThroughStore(string fault)
        {
            var draft = fault switch
            {
                "name-length" => new CampaignDraft(new string('a', 201), "custom", "Valid"),
                "system-length" => new CampaignDraft("Valid", "custom", new string('a', 101)),
                "direction" => new CampaignDraft("Hidden\u202Etext", "custom", "Valid"),
                "control" => new CampaignDraft("Valid", "custom", "Line\nBreak"),
                _ => new CampaignDraft("Bad\uD800", "custom", "Valid")
            };

            await Assert.ThrowsAsync<ArgumentException>(() => Store.CreateAsync(draft));

            Assert.IsFalse(Directory.Exists(CampaignRoot));
        }

        [TestMethod]
        [DataRow("name", "length")]
        [DataRow("customSystemName", "length")]
        [DataRow("name", "control")]
        [DataRow("customSystemName", "direction")]
        public async Task TamperedTextIsReportedWithoutChangingFile(string property, string fault)
        {
            var good = await Store.CreateAsync(new("Good", "custom", "Homebrew"));
            var bad = await Store.CreateAsync(new("Bad", "custom", "Homebrew"));
            var file = Path.Combine(CampaignRoot, bad.Id.ToString("D"), "campaign.json");
            var json = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
            json[property] = fault switch
            {
                "length" => new string('a', property == "name" ? 201 : 101),
                "control" => "Line\nBreak",
                _ => "Hidden\u202Etext"
            };
            var content = json.ToJsonString();
            await File.WriteAllTextAsync(file, content);
            var result = await Store.ListAsync();
            Assert.AreEqual(good, result.Campaigns.Single());
            Assert.HasCount(1, result.Issues);
            Assert.AreEqual(content, await File.ReadAllTextAsync(file));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public async Task MetadataByteLimitAcceptsExactBoundaryAndRejectsOverflow(int excess)
        {
            var good = await Store.CreateAsync(new("Good", "custom", "Homebrew"));
            var padded = await Store.CreateAsync(new("Padded", "custom", "Homebrew"));
            var file = Path.Combine(CampaignRoot, padded.Id.ToString("D"), "campaign.json");
            var original = await File.ReadAllBytesAsync(file);
            var bytes = Enumerable.Repeat((byte)' ', JsonCampaignStore.MaxMetadataBytes + excess).ToArray();
            original.CopyTo(bytes, 0);
            await File.WriteAllBytesAsync(file, bytes);
            var result = await Store.ListAsync();
            Assert.IsTrue(result.Campaigns.Contains(good));
            Assert.HasCount(excess == 0 ? 2 : 1, result.Campaigns);
            Assert.HasCount(excess, result.Issues);
            if (excess != 0) StringAssert.Contains(result.Issues.Single().Message, "64 KiB");
            CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(file));
        }

        [TestMethod]
        public async Task InjectionLikeTextRoundTripsAsDataInsideGuidFolder()
        {
            const string name = "../../outside; <script> & \"name\":\"override\" $(command)";
            const string system = "L'été — 東京 👩‍🚀";
            var created = await Store.CreateAsync(new(name, "custom", system));
            var loaded = (await Store.ListAsync()).Campaigns.Single();
            Assert.AreEqual(name, loaded.Name);
            Assert.AreEqual(system, loaded.CustomSystemName);
            CollectionAssert.AreEqual(new[] { created.Id.ToString("D") }, Directory.GetDirectories(CampaignRoot).Select(Path.GetFileName).ToArray());
        }
        [TestMethod]
        public async Task MissingDirectoryIsEmptyAndIsNotCreatedByReading()
        {
            var result = await Store.ListAsync();
            Assert.IsEmpty(result.Campaigns);
            Assert.IsEmpty(result.Issues);
            Assert.IsFalse(Directory.Exists(CampaignRoot));
        }

        [TestMethod]
        public async Task CreateTrimsCampaignAndSystemNames()
        {
            var campaign = await Store.CreateAsync(new("  The Ashen Crown  ", "custom", " Pathfinder 2e "));

            Assert.AreEqual("The Ashen Crown", campaign.Name);
            Assert.AreEqual("Pathfinder 2e", campaign.CustomSystemName);
        }

        [TestMethod]
        public async Task CreatePreservesRulesSystemIdentifier()
        {
            var campaign = await Store.CreateAsync(new("The Ashen Crown", "custom", "Pathfinder 2e"));

            Assert.AreEqual("custom", campaign.SystemId);
        }

        [TestMethod]
        public async Task CreateUsesClockForBothTimestamps()
        {
            var campaign = await Store.CreateAsync(new("The Ashen Crown", "custom", "Pathfinder 2e"));

            Assert.AreEqual(Now, campaign.CreatedAt);
            Assert.AreEqual(Now, campaign.UpdatedAt);
        }

        [TestMethod]
        public async Task ListRoundTripsMetadataWithoutRewritingDocument()
        {
            var campaign = await Store.CreateAsync(new("The Ashen Crown", "custom", "Pathfinder 2e"));
            var file = Path.Combine(CampaignRoot, campaign.Id.ToString("D"), "campaign.json");
            var before = await File.ReadAllTextAsync(file);

            var loaded = await new JsonCampaignStore(CampaignRoot).ListAsync();

            Assert.AreEqual(campaign, loaded.Campaigns.Single());
            Assert.AreEqual(before, await File.ReadAllTextAsync(file));
        }

        [TestMethod]
        public async Task SuccessfulCreateLeavesOnlyFinalDocument()
        {
            var campaign = await Store.CreateAsync(new("The Ashen Crown", "custom", "Pathfinder 2e"));

            var files = Directory.GetFiles(Path.Combine(CampaignRoot, campaign.Id.ToString("D")));

            CollectionAssert.AreEqual(new[] { "campaign.json" }, files.Select(Path.GetFileName).ToArray());
        }

        [TestMethod]
        public async Task CreateWritesSupportedFormatVersion()
        {
            var campaign = await Store.CreateAsync(new("The Ashen Crown", "custom", "Pathfinder 2e"));

            var content = await File.ReadAllTextAsync(Path.Combine(CampaignRoot, campaign.Id.ToString("D"), "campaign.json"));

            Assert.AreEqual(1, JsonNode.Parse(content)!["formatVersion"]!.GetValue<int>());
        }

        [TestMethod]
        public async Task DuplicateNamesUseDifferentFoldersAndDoNotOverwrite()
        {
            var first = await Store.CreateAsync(new("Same", "custom", "Homebrew"));
            var second = await Store.CreateAsync(new("Same", "custom", "Homebrew"));
            Assert.AreNotEqual(first.Id, second.Id);
            Assert.HasCount(2, (await Store.ListAsync()).Campaigns);
        }

        [TestMethod]
        [DataRow("", "custom", "Homebrew")]
        [DataRow("  ", "custom", "Homebrew")]
        [DataRow("Name", "custom", " ")]
        [DataRow("Name", "unknown", "System")]
        public async Task InvalidDraftDoesNotCreateFiles(string name, string system, string custom)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Store.CreateAsync(new(name, system, custom)));
            Assert.IsFalse(Directory.Exists(CampaignRoot));
        }

        [TestMethod]
        [DataRow("malformed")]
        [DataRow("version")]
        [DataRow("missingVersion")]
        [DataRow("wrongVersionType")]
        [DataRow("id")]
        [DataRow("name")]
        [DataRow("system")]
        [DataRow("customName")]
        [DataRow("created")]
        [DataRow("updated")]
        public async Task BadDocumentsAreReportedAndPreservedWhileValidCampaignsLoad(string fault)
        {
            var good = await Store.CreateAsync(new("Good", "custom", "Homebrew"));
            var bad = await Store.CreateAsync(new("Bad", "custom", "Homebrew"));
            var folder = Path.Combine(CampaignRoot, bad.Id.ToString("D"));
            var file = Path.Combine(folder, "campaign.json");
            var json = JsonNode.Parse(await File.ReadAllTextAsync(file))!.AsObject();
            switch (fault)
            {
                case "version": json["formatVersion"] = 99; break;
                case "missingVersion": json.Remove("formatVersion"); break;
                case "wrongVersionType": json["formatVersion"] = "bad"; break;
                case "id": json["id"] = Guid.NewGuid(); break;
                case "name": json.Remove("name"); break;
                case "system": json.Remove("systemId"); break;
                case "customName": json["customSystemName"] = null; break;
                case "created": json.Remove("createdAt"); break;
                case "updated": json.Remove("updatedAt"); break;
            }
            var content = fault == "malformed" ? "{broken" : json.ToJsonString();
            await File.WriteAllTextAsync(file, content);
            var result = await Store.ListAsync();
            Assert.AreEqual(good, result.Campaigns.Single());
            Assert.AreEqual(folder, result.Issues.Single().Folder);
            Assert.AreEqual(content, await File.ReadAllTextAsync(file));
        }

        [TestMethod]
        public async Task MissingDocumentReportsFolder()
        {
            var folder = Path.Combine(CampaignRoot, Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(folder);
            Assert.AreEqual(folder, (await Store.ListAsync()).Issues.Single().Folder);
        }

        [TestMethod]
        public async Task InvalidRootRaisesErrorRatherThanReturningEmptyLibrary()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CampaignRoot)!);
            await File.WriteAllTextAsync(CampaignRoot, "blocking file");
            await Assert.ThrowsAsync<IOException>(() => Store.ListAsync());
        }

        [TestMethod]
        public async Task FailedSaveDoesNotChangeExistingCampaigns()
        {
            var existing = await Store.CreateAsync(new("Existing", "custom", "Homebrew"));
            var file = Path.Combine(CampaignRoot, existing.Id.ToString("D"), "campaign.json");
            var before = await File.ReadAllTextAsync(file);
            var blockedRoot = Path.Combine(CampaignRoot, "blocked");
            await File.WriteAllTextAsync(blockedRoot, "not a directory");
            await Assert.ThrowsAsync<IOException>(() => new JsonCampaignStore(blockedRoot).CreateAsync(new("New", "custom", "Homebrew")));
            Assert.AreEqual(before, await File.ReadAllTextAsync(file));
            Assert.HasCount(1, (await Store.ListAsync()).Campaigns);
        }

        [TestMethod]
        public async Task MovingPortableDataRetainsCampaigns()
        {
            var campaign = await Store.CreateAsync(new("Portable", "custom", "Homebrew"));
            var movedRoot = Path.Combine(testRoot, "Moved application", "Data", "Campaigns");
            Directory.CreateDirectory(Path.GetDirectoryName(movedRoot)!);
            Directory.Move(CampaignRoot, movedRoot);
            var result = await new JsonCampaignStore(movedRoot).ListAsync();
            Assert.AreEqual(campaign, result.Campaigns.Single());
            Assert.IsEmpty(result.Issues);
        }
    }
}
