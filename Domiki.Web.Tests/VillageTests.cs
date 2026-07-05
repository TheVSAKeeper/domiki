using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class VillageTests : TestBase
    {
        [Test]
        public void GetVillageNewPlayerReturnsEmptyNameAndDefaultCrestTest()
        {
            var playerId = GetPlayerId();

            var village = GetVillage(playerId);

            Assert.That(village.VillageName, Is.Null);
            Assert.That(village.CrestIcon, Is.EqualTo(0));
            Assert.That(village.CrestColor, Is.EqualTo(0));
        }

        [Test]
        public void SetVillageValidSavesNormalizedNameAndCrestTest()
        {
            var playerId = GetPlayerId();

            SetVillage(playerId, "  Тихая   Долина  ", 2, 3);

            var village = GetVillage(playerId);
            Assert.That(village.VillageName, Is.EqualTo("Тихая Долина"));
            Assert.That(village.CrestIcon, Is.EqualTo(2));
            Assert.That(village.CrestColor, Is.EqualTo(3));
        }

        [Test]
        public void SetVillageDuplicateNameThrowsAndKeepsStateTest()
        {
            var firstPlayerId = GetPlayerId();
            var secondPlayerId = GetPlayerId();
            SetVillage(firstPlayerId, "Тихий Бор", 1, 2);
            SetVillage(secondPlayerId, "Ясная Поляна", 3, 4);

            Assert.Throws<BusinessException>(() => SetVillage(secondPlayerId, "Тихий Бор", 5, 6));

            var village = GetVillage(secondPlayerId);
            Assert.That(village.VillageName, Is.EqualTo("Ясная Поляна"));
            Assert.That(village.CrestIcon, Is.EqualTo(3));
            Assert.That(village.CrestColor, Is.EqualTo(4));
        }

        [TestCaseSource(nameof(InvalidLengthNames))]
        [TestCaseSource(nameof(InvalidCharacterNames))]
        [TestCaseSource(nameof(BlacklistedNames))]
        public void SetVillageInvalidNameThrowsTest(string name)
        {
            var playerId = GetPlayerId();

            Assert.Throws<BusinessException>(() => SetVillage(playerId, name, 0, 0));

            var village = GetVillage(playerId);
            Assert.That(village.VillageName, Is.Null);
        }

        [TestCase(-1, 0)]
        [TestCase(8, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 8)]
        public void SetVillageInvalidCrestThrowsTest(int crestIcon, int crestColor)
        {
            var playerId = GetPlayerId();

            Assert.Throws<BusinessException>(() => SetVillage(playerId, "Берёзки", crestIcon, crestColor));

            var village = GetVillage(playerId);
            Assert.That(village.VillageName, Is.Null);
        }

        public static IEnumerable<TestCaseData> InvalidLengthNames()
        {
            yield return new TestCaseData("Аб").SetName("TooShortVillageName");
            yield return new TestCaseData("1234567890123456789012345").SetName("TooLongVillageName");
            yield return new TestCaseData("   " ).SetName("BlankVillageName");
        }

        public static IEnumerable<TestCaseData> InvalidCharacterNames()
        {
            yield return new TestCaseData("Лес_Дом").SetName("UnderscoreVillageName");
            yield return new TestCaseData("Лес!").SetName("BangVillageName");
            yield return new TestCaseData("Лес/Дом").SetName("SlashVillageName");
            yield return new TestCaseData("Лес\tДом").SetName("TabVillageName");
        }

        public static IEnumerable<TestCaseData> BlacklistedNames()
        {
            yield return new TestCaseData("Админово").SetName("AdminVillageName");
            yield return new TestCaseData("Модераторск").SetName("ModeratorVillageName");
            yield return new TestCaseData("Домикино").SetName("DomikiVillageName");
            yield return new TestCaseData("fucktown").SetName("ProfanityLatinVillageName");
            yield return new TestCaseData("сукадеревня").SetName("ProfanityCyrillicVillageName");
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private Village GetVillage(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var village = domikManager.GetVillage(playerId);
                uow.Commit();
                return village;
            }
        }

        private void SetVillage(int playerId, string name, int crestIcon, int crestColor)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.SetVillageIdentity(playerId, name, crestIcon, crestColor);
                uow.Commit();
            }
        }
    }
}
