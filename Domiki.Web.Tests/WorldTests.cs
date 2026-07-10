using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;
using Domiki.Web.Models;
using System.Text.Json;

namespace Domiki.Web.Tests
{
    public class WorldTests : TestBase
    {
        [Test]
        public void GetWorldReturnsNamedVillagesAndNpcsSortedByLevelTest()
        {
            var lowPlayerId = CreateNamedPlayer("Мир Низина", 0, 1);
            var middlePlayerId = CreateNamedPlayer("Мир Средняя", 1, 2);
            BuyDomik(middlePlayerId, 2);
            var unnamedPlayerId = GetPlayerId();

            var world = GetWorld(lowPlayerId);

            Assert.That(world.Villages.Select(x => x.Level), Is.EqualTo(world.Villages.Select(x => x.Level).OrderByDescending(x => x)));
            Assert.That(world.Villages.Any(x => x.PlayerId == lowPlayerId), Is.True);
            Assert.That(world.Villages.Any(x => x.PlayerId == middlePlayerId), Is.True);
            Assert.That(world.Villages.Any(x => x.PlayerId == unnamedPlayerId), Is.False);
            Assert.That(Array.IndexOf(world.Villages, world.Villages.Single(x => x.PlayerId == middlePlayerId)), Is.LessThan(Array.IndexOf(world.Villages, world.Villages.Single(x => x.PlayerId == lowPlayerId))));
        }

        [Test]
        public void GetWorldMarksOnlyCurrentPlayerAsMeTest()
        {
            var mePlayerId = CreateNamedPlayer("Мир Своя", 2, 3);
            var otherPlayerId = CreateNamedPlayer("Мир Чужая", 3, 4);

            var world = GetWorld(mePlayerId);

            Assert.That(world.Villages.Single(x => x.PlayerId == mePlayerId).IsMe, Is.True);
            Assert.That(world.Villages.Single(x => x.PlayerId == otherPlayerId).IsMe, Is.False);
            Assert.That(world.Villages.Where(x => x.IsMe).Select(x => x.PlayerId), Is.EqualTo(new int?[] { mePlayerId }));
        }

        [Test]
        public void GetWorldReturnsNpcRowsWithPresentationConstantsTest()
        {
            var playerId = GetPlayerId();

            var world = GetWorld(playerId);
            var npcs = world.Villages.Where(x => x.IsNpc).ToArray();

            Assert.That(npcs.Length, Is.EqualTo(5));
            AssertNpc(npcs, "Заречье", 45, 2, 2, 6);
            AssertNpc(npcs, "Боровое", 38, 1, 0, 7);
            AssertNpc(npcs, "Каменка", 30, 3, 4, 2);
            AssertNpc(npcs, "Глинищи", 24, 6, 1, 4);
            AssertNpc(npcs, "Дубрава", 18, 4, 5, 3);
            Assert.That(npcs.All(x => x.PlayerId == null), Is.True);
            Assert.That(npcs.All(x => !x.IsMe), Is.True);
        }

        [Test]
        public void VisitVillageReturnsPublicFieldsAndBuildingsTest()
        {
            var playerId = CreateNamedPlayer("Мир Визит", 4, 5);
            BuyDomik(playerId, 2);

            var visit = VisitVillage(playerId);

            Assert.That(visit.VillageName, Does.StartWith("Мир Визит"));
            Assert.That(visit.CrestIcon, Is.EqualTo(4));
            Assert.That(visit.CrestColor, Is.EqualTo(5));
            Assert.That(visit.Level.Level, Is.GreaterThan(0));
            Assert.That(visit.Buildings.Any(x => x.TypeName == "Барак" && x.Level == 1), Is.True);
        }

        [Test]
        public void WorldDtosDoNotContainPrivatePlayerFieldsTest()
        {
            var playerId = CreateNamedPlayer("Мир Приватность", 5, 6);
            BuyDomik(playerId, 2);

            WorldDto worldDto;
            VillageVisitDto visitDto;
            using (var uow = GetUow())
            {
                var manager = GetWorldManager(uow);
                worldDto = manager.GetWorld(playerId).ToDto();
                visitDto = manager.VisitVillage(playerId).ToDto();
                uow.Commit();
            }

            var worldJson = JsonSerializer.Serialize(worldDto);
            var worldVillageJson = JsonSerializer.Serialize(worldDto.Villages.Single(x => x.PlayerId == playerId));
            var visitJson = JsonSerializer.Serialize(visitDto);

            AssertPrivateFieldsAbsent(worldJson);
            AssertPrivateFieldsAbsent(worldVillageJson);
            AssertPrivateFieldsAbsent(visitJson);
        }

        [Test]
        public void VisitVillageMissingOrUnnamedPlayerThrowsTest()
        {
            var unnamedPlayerId = GetPlayerId();

            var missingEx = Assert.Throws<BusinessException>(() => VisitVillage(int.MaxValue));
            var unnamedEx = Assert.Throws<BusinessException>(() => VisitVillage(unnamedPlayerId));

            Assert.That(missingEx.Message, Is.EqualTo("Деревня не найдена"));
            Assert.That(unnamedEx.Message, Is.EqualTo("Деревня не найдена"));
        }

        private static void AssertPrivateFieldsAbsent(string json)
        {
            Assert.That(json, Does.Not.Contain("\"Name\""));
            Assert.That(json, Does.Not.Contain("AspNetUserId"));
        }

        private static void AssertNpc(WorldVillage[] npcs, string name, int level, int crestIcon, int crestColor, int resourceTypeId)
        {
            var npc = npcs.Single(x => x.VillageName == name);
            Assert.That(npc.Level, Is.EqualTo(level));
            Assert.That(npc.CrestIcon, Is.EqualTo(crestIcon));
            Assert.That(npc.CrestColor, Is.EqualTo(crestColor));
            Assert.That(npc.NpcResourceTypeId, Is.EqualTo(resourceTypeId));
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

        private int CreateNamedPlayer(string prefix, int crestIcon, int crestColor)
        {
            var playerId = GetPlayerId();
            SetVillage(playerId, TestVillageName(prefix), crestIcon, crestColor);
            return playerId;
        }

        private World GetWorld(int currentPlayerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetWorldManager(uow);
                var world = manager.GetWorld(currentPlayerId);
                uow.Commit();
                return world;
            }
        }

        private VillageVisit VisitVillage(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetWorldManager(uow);
                var visit = manager.VisitVillage(playerId);
                uow.Commit();
                return visit;
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

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }
    }
}
