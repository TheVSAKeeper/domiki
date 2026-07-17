using Domiki.Web.Data.Entities;
using Domiki.Web.Economy;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Models;
using Domiki.Web.Reference;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class GiftTests
{
    /// <summary>
    /// Сосед занимает полуинтервал кумулятивного веса: бросок ровно на границе достаётся следующему соседу.
    /// </summary>
    /// <param name="weights">Веса соседей.</param>
    /// <param name="roll">Детерминированный бросок.</param>
    /// <param name="expectedIndex">Ожидаемый индекс соседа.</param>
    [TestCase(new[] { 1, 3 }, 0.0, 0)]
    [TestCase(new[] { 1, 3 }, 0.24, 0)]
    [TestCase(new[] { 1, 3 }, 0.25, 1)]
    [TestCase(new[] { 1, 3 }, 0.26, 1)]
    [TestCase(new[] { 1, 3 }, 0.99, 1)]
    [TestCase(new[] { 1 }, 0.99, 0)]
    public void WeightedIndexTest(int[] weights, double roll, int expectedIndex)
    {
        Assert.That(GiftManager.PickWeightedIndex(weights, roll), Is.EqualTo(expectedIndex));
    }

    /// <summary>
    /// Вес соседа растёт на единицу с каждыми двадцатью пятью очками репутации и упирается в тройку.
    /// </summary>
    /// <param name="points">Репутация с соседом.</param>
    /// <param name="expectedWeight">Ожидаемый вес соседа при выборе дарящего.</param>
    [TestCase(0, 1)]
    [TestCase(24, 1)]
    [TestCase(25, 2)]
    [TestCase(49, 2)]
    [TestCase(50, 3)]
    [TestCase(100, 3)]
    [TestCase(-5, 1)]
    public void RepWeightTest(int points, int expectedWeight)
    {
        Assert.That(GiftManager.RepWeight(points), Is.EqualTo(expectedWeight));
    }

    /// <summary>
    /// После отлучки на семь часов соседский гостинец стоит не меньше сорока монет, попадает в сводку и начинает счётчик
    /// визитов с единицы.
    /// </summary>
    [Test]
    public void LongAbsenceGrantsGiftTest()
    {
        const int awayHours = 7;
        const int minimumMarketValue = 40;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        SetLastSeen(player.Id, now.AddHours(-awayHours));

        player.TryGrantGift(now);

        var recap = player.TakeRecap(now);
        var gift = recap.Events.Single(x => x.Type == PlayerEventType.NeighborGift);
        var resource = gift.Data.GetProperty("resources").EnumerateArray().Single();
        var resourceTypeId = resource.GetProperty("resourceTypeId").GetInt32();
        var value = resource.GetProperty("value").GetInt32();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResourceManager.GetMarketValue(resourceTypeId) * value, Is.GreaterThanOrEqualTo(minimumMarketValue));
            Assert.That(new[] { ResourceIds.Stone, ResourceIds.Block, ResourceIds.Clay, ResourceIds.Dishes, ResourceIds.Wood, ResourceIds.Bread }, Does.Contain(resourceTypeId));
            Assert.That(gift.Data.GetProperty("visitIndex").GetInt32(), Is.EqualTo(1));
            Assert.That(VisitsSinceBigGift(player.Id), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// После гостинца счётчик визитов отображается в состоянии обжитости.
    /// </summary>
    [Test]
    public void VisitsVisibleInLevelTest()
    {
        const int awayHours = 7;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        SetLastSeen(player.Id, now.AddHours(-awayHours));

        player.TryGrantGift(now);

        Assert.That(player.GetVillageLevel().VisitsSinceBigGift, Is.EqualTo(1));
    }

    /// <summary>
    /// Игрок без даты последнего посещения не получает гостинец и сохраняет нулевой счётчик визитов.
    /// </summary>
    [Test]
    public void MissingLastSeenDoesNotGrantGiftTest()
    {
        var player = TestPlayer.Create();
        SetLastSeen(player.Id, null);

        player.TryGrantGift(DateTimeHelper.GetNowDate());

        var recap = player.TakeRecap(DateTimeHelper.GetNowDate());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(recap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.NeighborGift));
            Assert.That(VisitsSinceBigGift(player.Id), Is.Zero);
        }
    }

    /// <summary>
    /// Повторная проверка без второй отлучки не создаёт второго гостинца и оставляет счётчик на первом визите.
    /// </summary>
    [Test]
    public void RecapPreventsRepeatedGiftTest()
    {
        const int awayHours = 7;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        SetLastSeen(player.Id, now.AddHours(-awayHours));

        player.TryGrantGift(now);
        var firstRecap = player.TakeRecap(now);
        player.TryGrantGift(now);
        var secondRecap = player.TakeRecap(now);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstRecap.Events.Select(x => x.Type), Does.Contain(PlayerEventType.NeighborGift));
            Assert.That(secondRecap.Events, Is.Empty);
            Assert.That(VisitsSinceBigGift(player.Id), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Репутация не ниже тридцати у обоих открытых соседей увеличивает рыночную стоимость гостинца минимум до шестидесяти
    /// монет.
    /// </summary>
    [Test]
    public void ReputationIncreasesGiftValueTest()
    {
        const int reputationPoints = 30;
        const int awayHours = 7;
        const int minimumMarketValue = 60;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        SetLastSeen(player.Id, now.AddHours(-awayHours));
        SetReputation(player.Id, NeighborIds.Zarechye, reputationPoints);
        SetReputation(player.Id, NeighborIds.Borovoe, reputationPoints);
        SetReputation(player.Id, NeighborIds.Kamenka, reputationPoints);
        SetReputation(player.Id, NeighborIds.Glinischi, reputationPoints);
        SetReputation(player.Id, NeighborIds.Dubrava, reputationPoints);

        player.TryGrantGift(now);

        var recap = player.TakeRecap(now);
        var gift = recap.Events.Single(x => x.Type == PlayerEventType.NeighborGift);
        var resource = gift.Data.GetProperty("resources").EnumerateArray().Single();
        var resourceTypeId = resource.GetProperty("resourceTypeId").GetInt32();
        var value = resource.GetProperty("value").GetInt32();

        Assert.That(ResourceManager.GetMarketValue(resourceTypeId) * value, Is.GreaterThanOrEqualTo(minimumMarketValue));
    }

    /// <summary>
    /// Седьмой гостинец выдаёт один декор из праздничного пула, сбрасывает счётчик и несёт в событии признак большого
    /// гостинца.
    /// </summary>
    [Test]
    public void SeventhGiftGrantsDecorTest()
    {
        const int visitsBeforeBigGift = 6;
        const int awayHours = 7;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        SetLastSeen(player.Id, now.AddHours(-awayHours));
        SetVisitsSinceBigGift(player.Id, visitsBeforeBigGift);

        player.TryGrantGift(now);

        var recap = player.TakeRecap(now);
        var gift = recap.Events.Single(x => x.Type == PlayerEventType.NeighborGift);
        var decors = PlayerDecorIds(player.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(decors.Length, Is.EqualTo(1));
            Assert.That(new[] { DecorIds.Flowerbed, DecorIds.Bench, DecorIds.Lantern }, Does.Contain(decors.Single()));
            Assert.That(VisitsSinceBigGift(player.Id), Is.Zero);
            Assert.That(gift.Data.GetProperty("big").GetBoolean(), Is.True);
            Assert.That(gift.Data.GetProperty("visitIndex").GetInt32(), Is.EqualTo(7));
        }
    }

    /// <summary>
    /// Отлучка на один час не меняет ресурсы, не создаёт гостинец и не увеличивает счётчик визитов.
    /// </summary>
    [Test]
    public void ShortAbsenceDoesNotGrantGiftTest()
    {
        const int awayHours = 1;

        var player = TestPlayer.Create();
        var now = DateTimeHelper.GetNowDate();
        var resourcesBefore = player.Resources().Select(x => (x.Type.Id, x.Value)).OrderBy(x => x.Id).ToArray();
        SetLastSeen(player.Id, now.AddHours(-awayHours));

        player.TryGrantGift(now);

        var recap = player.TakeRecap(now);
        var resourcesAfter = player.Resources().Select(x => (x.Type.Id, x.Value)).OrderBy(x => x.Id).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resourcesAfter, Is.EqualTo(resourcesBefore));
            Assert.That(recap.Events.Select(x => x.Type), Does.Not.Contain(PlayerEventType.NeighborGift));
            Assert.That(VisitsSinceBigGift(player.Id), Is.Zero);
        }
    }

    private static void SetLastSeen(int playerId, DateTime? lastSeen)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).LastSeen = lastSeen;
        scope.Commit();
    }

    private static void SetVisitsSinceBigGift(int playerId, int visitsSinceBigGift)
    {
        using var scope = App.Scope();
        scope.Context.Players.Single(x => x.Id == playerId).VisitsSinceBigGift = visitsSinceBigGift;
        scope.Commit();
    }

    private static void SetReputation(int playerId, int neighborId, int points)
    {
        using var scope = App.Scope();
        var reputation = scope.Context.NeighborReputations.SingleOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
        if (reputation == null)
        {
            reputation = new()
            {
                PlayerId = playerId,
                NeighborId = neighborId,
            };

            scope.Context.NeighborReputations.Add(reputation);
        }

        reputation.Points = points;
        scope.Commit();
    }

    private static int VisitsSinceBigGift(int playerId)
    {
        return App.Read(context => context.Players.Single(x => x.Id == playerId).VisitsSinceBigGift);
    }

    private static int[] PlayerDecorIds(int playerId)
    {
        return App.Read(context => context.PlayerDecors.Where(x => x.PlayerId == playerId).Select(x => x.DecorTypeId).ToArray());
    }
}

file static class GiftTestsActs
{
    public static void TryGrantGift(this TestPlayer player, DateTime now)
    {
        App.Act<GiftManager>(m => m.TryGrantGift(player.Id, now));
    }

    public static RecapModel TakeRecap(this TestPlayer player, DateTime now)
    {
        return App.Act<PlayerEventManager, RecapModel>(m => m.TakeRecap(player.Id, now));
    }
}
