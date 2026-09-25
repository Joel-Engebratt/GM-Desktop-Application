using GM_Desktop_Application.Models;

namespace GM.Desktop.Tests.Models
{
    [TestClass]
    public sealed class CampaignDraftTests
    {
        [TestMethod]
        [DataRow(200, true)]
        [DataRow(201, false)]
        public void NameLengthUsesUtf16Limit(int length, bool accepted)
        {
            var draft = new CampaignDraft(new string('a', length), "custom", "Valid");

            Assert.AreEqual(accepted, draft.NameError is null);
        }

        [TestMethod]
        [DataRow(100, true)]
        [DataRow(101, false)]
        public void SystemNameLengthUsesUtf16Limit(int length, bool accepted)
        {
            var draft = new CampaignDraft("Valid", "custom", new string('b', length));

            Assert.AreEqual(accepted, draft.CustomSystemError is null);
        }

        [TestMethod]
        public void NameLengthIsCheckedBeforeTrimming() =>
            Assert.IsNotNull(new CampaignDraft(new string(' ', 200) + "a", "custom", "Valid").NameError);

        [TestMethod]
        [DataRow(100, true)]
        [DataRow(101, false)]
        public void EmojiNamesCountBothUtf16Units(int emojiCount, bool accepted)
        {
            var name = string.Concat(Enumerable.Repeat("🐉", emojiCount));

            Assert.AreEqual(accepted, new CampaignDraft(name, "custom", "Valid").NameError is null);
        }

        public static IEnumerable<object[]> ForbiddenCharacters()
        {
            var forbidden = Enumerable.Range(0, 32).Concat(Enumerable.Range(127, 33))
                .Concat([0x061C, 0x200E, 0x200F, 0x2028, 0x2029])
                .Concat(Enumerable.Range(0x202A, 5)).Concat(Enumerable.Range(0x2066, 10));
            foreach (var code in forbidden)
            {
                yield return [code, false];
                yield return [code, true];
            }
        }

        [TestMethod]
        [DynamicData(nameof(ForbiddenCharacters))]
        public void ControlOrDirectionCharacterIsRejected(int code, bool systemName)
        {
            var text = "Before" + (char)code + "After";
            var draft = systemName ? new CampaignDraft("Valid", "custom", text) : new CampaignDraft(text, "custom", "Valid");

            Assert.IsNotNull(systemName ? draft.CustomSystemError : draft.NameError, $"U+{code:X4}");
        }

        [TestMethod]
        public void TrimmingCannotHideControlCharacters() =>
            Assert.IsFalse(new CampaignDraft("\tName\n", "custom", "Valid").IsValid);

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
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(2, false)]
        [DataRow(0, true)]
        [DataRow(1, true)]
        [DataRow(2, true)]
        public void UnpairedSurrogateIsRejected(int sample, bool systemName)
        {
            var text = new[] { "a\uD800", "\uDC00a", "\uD800\uD800" }[sample];
            var draft = systemName ? new CampaignDraft("Valid", "custom", text) : new CampaignDraft(text, "custom", "Valid");

            Assert.IsNotNull(systemName ? draft.CustomSystemError : draft.NameError);
        }
    }
}
