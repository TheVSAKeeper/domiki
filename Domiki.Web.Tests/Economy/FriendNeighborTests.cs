using Domiki.Web.Economy;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

public sealed class FriendNeighborTests
{
    /// <summary>
    /// Дружбу с ещё не открытым по обжитости соседом назначить нельзя, с открытым – можно, а <see langword="null"/> её снимает.
    /// </summary>
    [Test]
    public void SetFriendNeighborRejectsClosedAcceptsOpenAndClearsWithNullTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.SetFriendNeighbor(NeighborIds.Zarechye));
        Assert.That(ex.Message, Is.EqualTo("С этой деревней вы пока не знакомы – дорога к ней откроется с ростом обжитости."));

        player.SetFriendNeighbor(NeighborIds.Glinischi);
        Assert.That(player.Reputation().Single(x => x.IsFriend).Neighbor.Id, Is.EqualTo(NeighborIds.Glinischi));

        player.SetFriendNeighbor(null);
        Assert.That(player.Reputation().Any(x => x.IsFriend), Is.False);
    }

    /// <summary>
    /// Заказы соседа, с которым игрок водит дружбу, занимают заметно больше трети доски: на трёх равнодоступных
    /// ресурсах и открытых соседях (без дружбы доля друга была бы 1/3) ожидаемая доля – около 65%.
    /// </summary>
    [Test]
    public void FriendNeighborOrdersAppearMoreOftenOnBoardTest()
    {
        const int refillCycles = 40;
        const double minimumFriendShare = 0.45;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.LumberMill)
            .WithDomik(DomikIds.StoneMine)
            .SetFriendNeighbor(NeighborIds.Glinischi);

        RaiseVillageLevelTo(player.Id, 3);

        var friendCount = 0;
        var totalCount = 0;

        for (var i = 0; i < refillCycles; i++)
        {
            var orders = player.Orders();
            totalCount += orders.Count;
            friendCount += orders.Count(x => x.Neighbor.Id == NeighborIds.Glinischi);
            ClearOrders(player.Id);
        }

        Assert.That((double)friendCount / totalCount, Is.GreaterThan(minimumFriendShare));
    }

    /// <summary>
    /// Сосед, с которым водят дружбу, не занимает всю доску: хотя бы одна из трёх весточек всегда приходит от других выселок.
    /// </summary>
    [Test]
    public void FriendNeighborNeverFillsWholeBoardTest()
    {
        const int refillCycles = 40;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.LumberMill)
            .WithDomik(DomikIds.StoneMine)
            .SetFriendNeighbor(NeighborIds.Glinischi);

        RaiseVillageLevelTo(player.Id, 3);

        for (var i = 0; i < refillCycles; i++)
        {
            var friendOrders = player.Orders().Count(x => x.Neighbor.Id == NeighborIds.Glinischi);
            Assert.That(friendOrders, Is.LessThanOrEqualTo(OrderManager.FriendBoardLimit));
            ClearOrders(player.Id);
        }
    }

    /// <summary>
    /// Ближайший недостигнутый порог репутации у соседа назван правильно и по мере роста репутации переходит к следующему.
    /// </summary>
    /// <param name="points">Очки репутации у соседа.</param>
    /// <param name="expectedThreshold">Ожидаемый ближайший недостигнутый порог.</param>
    /// <param name="expectedName">Ожидаемое название того, что открывает ближайший порог.</param>
    [TestCase(0, 5, "обоз соседа")]
    [TestCase(5, 15, "«Чертёж гончарни»")]
    [TestCase(15, 20, "второй товар в обозе")]
    [TestCase(20, 40, "щедрый обоз")]
    public void NextReputationMilestoneNamesRealRewardTest(int points, int expectedThreshold, string expectedName)
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, points);

        var reputation = player.Reputation().First(x => x.Neighbor.Id == NeighborIds.Glinischi);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reputation.NextThreshold, Is.EqualTo(expectedThreshold));
            Assert.That(reputation.NextRewardName, Is.EqualTo(expectedName));
        }
    }

    /// <summary>
    /// Когда игрок прошёл все известные пороги соседа, ближайший порог и его название не заполняются.
    /// </summary>
    [Test]
    public void NextReputationMilestoneIsNullPastEveryThresholdTest()
    {
        var player = TestPlayer.Create()
            .WithReputation(NeighborIds.Glinischi, ConvoyManager.HighLimitReputationThreshold);

        var reputation = player.Reputation().First(x => x.Neighbor.Id == NeighborIds.Glinischi);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reputation.NextThreshold, Is.Null);
            Assert.That(reputation.NextRewardName, Is.Null);
        }
    }

    private static void ClearOrders(int playerId)
    {
        using var scope = App.Scope();
        var orders = scope.Context.Orders.Where(x => x.PlayerId == playerId).ToArray();
        var orderIds = orders.Select(x => x.Id).ToArray();
        var resources = scope.Context.OrderResources.Where(x => orderIds.Contains(x.OrderId)).ToArray();
        scope.Context.OrderResources.RemoveRange(resources);
        scope.Context.Orders.RemoveRange(orders);
        scope.Commit();
    }

    private static void RaiseVillageLevelTo(int playerId, int targetLevel)
    {
        var current = App.Act<VillageLevelCalculator, int>(m => m.GetLevel(playerId).Level);
        if (current >= targetLevel)
        {
            return;
        }

        var milestones = (int)Math.Ceiling((targetLevel - current) / (double)VillageLevelCalculator.ReputationWeight);
        GrantReputation(playerId, NeighborIds.Zarechye, milestones * VillageLevelCalculator.ReputationPointsPerMilestone);
    }

    private static void GrantReputation(int playerId, int neighborId, int points)
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

        reputation.Points += points;
        scope.Commit();
    }
}
