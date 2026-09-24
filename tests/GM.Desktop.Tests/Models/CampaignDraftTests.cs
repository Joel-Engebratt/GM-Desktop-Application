using GM_Desktop_Application.Models;

namespace GM.Desktop.Tests.Models
{
    [TestClass]
    public sealed class CampaignDraftTests
    {
        [TestMethod]
        public void LengthLimitsAcceptBoundaryAndRejectOverflowIncludingPadding()
        {
            Assert.IsTrue(new CampaignDraft(new string('a', 200), "custom", new string('b', 100)).IsValid);
            Assert.IsNotNull(new CampaignDraft(new string('a', 201), "custom", "Valid").NameError);
            Assert.IsNotNull(new CampaignDraft("Valid", "custom", new string('b', 101)).CustomSystemError);
            Assert.IsFalse(new CampaignDraft(new string(' ', 200) + "a", "custom", "Valid").IsValid);
            Assert.IsTrue(new CampaignDraft(string.Concat(Enumerable.Repeat("🐉", 100)), "custom", "Valid").IsValid);
            Assert.IsFalse(new CampaignDraft(string.Concat(Enumerable.Repeat("🐉", 101)), "custom", "Valid").IsValid);
        }

        [TestMethod]
        public void ControlAndDirectionCharactersAreRejectedInBothFields()
        {
            var forbidden = Enumerable.Range(0, 32).Concat(Enumerable.Range(127, 33))
                .Concat([0x061C, 0x200E, 0x200F, 0x2028, 0x2029])
                .Concat(Enumerable.Range(0x202A, 5)).Concat(Enumerable.Range(0x2066, 10));
            foreach (var code in forbidden)
            {
                var text = "Before" + (char)code + "After";
                Assert.IsNotNull(new CampaignDraft(text, "custom", "Valid").NameError, $"U+{code:X4}");
                Assert.IsNotNull(new CampaignDraft("Valid", "custom", text).CustomSystemError, $"U+{code:X4}");
            }
            Assert.IsFalse(new CampaignDraft("\tName\n", "custom", "Valid").IsValid);
        }

        [TestMethod]
        [DataRow("L'été d'Åsa — 東京")]
        [DataRow("حكاية المغامرة")]
        [DataRow("שלום עולם")]
        [DataRow("Cafe\u0301 🐉 👩‍🚀")]
        [DataRow("نام\u200Cها")]
        [DataRow("<script>alert('test')</script> & \"quoted\"")]
        [DataRow("../../outside; $(command)")]
        public void OrdinaryUnicodeAndCodeLookingTextRemainLiteralNames(string text)
        {
            Assert.IsTrue(new CampaignDraft(text, "custom", text).IsValid);
        }

        [TestMethod]
        public void UnpairedSurrogatesAreRejectedInsteadOfBeingSilentlyReplaced()
        {
            foreach (var text in new[] { "a\uD800", "\uDC00a", "\uD800\uD800" })
            {
                Assert.IsFalse(new CampaignDraft(text, "custom", "Valid").IsValid);
                Assert.IsFalse(new CampaignDraft("Valid", "custom", text).IsValid);
            }
        }
    }
}
