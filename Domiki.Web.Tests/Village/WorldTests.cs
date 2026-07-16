using Domiki.Web.Infrastructure;
using Domiki.Web.Village.Models;
using System.Text.Json;

namespace Domiki.Web.Tests;

public sealed class WorldTests
{
    /// <summary>
    /// В списке мира флагом «своя» помечена ровно одна деревня – деревня запросившего игрока.
    /// </summary>
    [Test]
    public void GetWorldMarksOnlyCurrentPlayerAsMeTest()
    {
        var me = TestPlayer.Create()
            .SetVillageIdentity("Мир Своя-" + Guid.NewGuid().ToString("N")[..6], 2, 3);

        var other = TestPlayer.Create()
            .SetVillageIdentity("Мир Чужая-" + Guid.NewGuid().ToString("N")[..6], 3, 4);

        var world = me.World();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(world.Villages.Single(x => x.PlayerId == me.Id).IsMe, Is.True);
            Assert.That(world.Villages.Single(x => x.PlayerId == other.Id).IsMe, Is.False);
            Assert.That(world.Villages.Where(x => x.IsMe).Select(x => x.PlayerId), Is.EqualTo(new int?[] { me.Id }));
        }
    }

    /// <summary>
    /// Список мира включает только именованные деревни, отсортированные по уровню по убыванию, и не включает неназванных
    /// игроков.
    /// </summary>
    [Test]
    public void GetWorldReturnsNamedVillagesAndNpcsSortedByLevelTest()
    {
        var low = TestPlayer.Create()
            .SetVillageIdentity("Мир Низина-" + Guid.NewGuid().ToString("N")[..6], 0, 1);

        var middle = TestPlayer.Create()
            .SetVillageIdentity("Мир Средняя-" + Guid.NewGuid().ToString("N")[..6], 1, 2)
            .WithDomik(DomikIds.Barrack);

        var unnamed = TestPlayer.Create();

        var world = low.World();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(world.Villages.Select(x => x.Level), Is.EqualTo(world.Villages.Select(x => x.Level).OrderByDescending(x => x)));
            Assert.That(world.Villages.Any(x => x.PlayerId == low.Id), Is.True);
            Assert.That(world.Villages.Any(x => x.PlayerId == middle.Id), Is.True);
            Assert.That(world.Villages.Any(x => x.PlayerId == unnamed.Id), Is.False);
            Assert.That(Array.IndexOf(world.Villages, world.Villages.Single(x => x.PlayerId == middle.Id)), Is.LessThan(Array.IndexOf(world.Villages, world.Villages.Single(x => x.PlayerId == low.Id))));
        }
    }

    /// <summary>
    /// Список мира всегда содержит фиксированный набор из пяти деревень-NPC с заранее заданными именами, уровнями, гербами и
    /// типами ресурсов.
    /// </summary>
    [Test]
    public void GetWorldReturnsNpcRowsWithPresentationConstantsTest()
    {
        var player = TestPlayer.Create();

        var world = player.World();
        var npcs = world.Villages.Where(x => x.IsNpc).ToArray();

        Assert.That(npcs.Length, Is.EqualTo(5));
        AssertNpc(npcs, "Заречье", "zarechye", 45, 2, 2, 6);
        AssertNpc(npcs, "Боровое", "borovoe", 38, 1, 0, 7);
        AssertNpc(npcs, "Каменка", "kamenka", 30, 3, 4, 2);
        AssertNpc(npcs, "Глинищи", "glinischi", 24, 6, 1, 4);
        AssertNpc(npcs, "Дубрава", "dubrava", 18, 4, 5, 3);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(npcs.All(x => x.PlayerId == null), Is.True);
            Assert.That(npcs.All(x => !x.IsMe), Is.True);
        }
    }

    /// <summary>
    /// Визит к несуществующему игроку и к игроку без названной деревни одинаково падает с ошибкой «Деревня не найдена».
    /// </summary>
    [Test]
    public void VisitVillageMissingOrUnnamedPlayerThrowsTest()
    {
        var unnamed = TestPlayer.Create();

        var missingEx = Throws.Business(() => VillageActs.VisitPlayer(int.MaxValue));
        var unnamedEx = Throws.Business(() => unnamed.Visit());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingEx.Message, Is.EqualTo("Деревня не найдена"));
            Assert.That(unnamedEx.Message, Is.EqualTo("Деревня не найдена"));
        }
    }

    /// <summary>
    /// Визит в чужую деревню отдаёт её публичную идентичность, уровень и список построек без права владения.
    /// </summary>
    [Test]
    public void VisitVillageReturnsPublicFieldsAndBuildingsTest()
    {
        var villageName = "Мир Визит-" + Guid.NewGuid().ToString("N")[..6];

        var player = TestPlayer.Create()
            .SetVillageIdentity(villageName, 4, 5)
            .WithDomik(DomikIds.Barrack);

        var visit = player.Visit();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(visit.VillageName, Does.StartWith("Мир Визит"));
            Assert.That(visit.CrestIcon, Is.EqualTo(4));
            Assert.That(visit.CrestColor, Is.EqualTo(5));
            Assert.That(visit.Level.Level, Is.GreaterThan(0));
            Assert.That(visit.Buildings.Any(x => x.TypeName == "Артельная изба" && x.Level == 1), Is.True);
        }
    }

    /// <summary>
    /// DTO мира и визита в деревню не утекают приватные поля игрока (Name, AspNetUserId) в сериализованный JSON.
    /// </summary>
    [Test]
    public void WorldDtosDoNotContainPrivatePlayerFieldsTest()
    {
        var player = TestPlayer.Create()
            .SetVillageIdentity("Мир Приватность-" + Guid.NewGuid().ToString("N")[..6], 5, 6)
            .WithDomik(DomikIds.Barrack);

        var worldDto = player.WorldDto();
        var visitDto = player.VisitDto();

        var worldJson = JsonSerializer.Serialize(worldDto);
        var worldVillageJson = JsonSerializer.Serialize(worldDto.Villages.Single(x => x.PlayerId == player.Id));
        var visitJson = JsonSerializer.Serialize(visitDto);

        AssertPrivateFieldsAbsent(worldJson);
        AssertPrivateFieldsAbsent(worldVillageJson);
        AssertPrivateFieldsAbsent(visitJson);
    }

    private static void AssertPrivateFieldsAbsent(string json)
    {
        Assert.That(json, Does.Not.Contain("\"Name\""));
        Assert.That(json, Does.Not.Contain("AspNetUserId"));
    }

    private static void AssertNpc(WorldVillage[] npcs, string name, string logicName, int level, int crestIcon, int crestColor, int resourceTypeId)
    {
        var npc = npcs.Single(x => x.VillageName == name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(npc.NpcLogicName, Is.EqualTo(logicName));
            Assert.That(npc.Level, Is.EqualTo(level));
            Assert.That(npc.CrestIcon, Is.EqualTo(crestIcon));
            Assert.That(npc.CrestColor, Is.EqualTo(crestColor));
            Assert.That(npc.NpcResourceTypeId, Is.EqualTo(resourceTypeId));
        }
    }
}
