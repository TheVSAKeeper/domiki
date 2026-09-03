using Domiki.Web.Core;
using Domiki.Web.Economy;

namespace Domiki.Web.Tests;

public sealed class PostHouseTests
{
    /// <summary>
    /// «Ямская изба» первого уровня стоит у игрока с самого начала – её не покупают и не открывают обжитостью.
    /// </summary>
    [Test]
    public void NewPlayerHasPostHouseTest()
    {
        var player = TestPlayer.Create();

        Assert.That(player.PostHouseLevel(), Is.EqualTo(1));
    }

    /// <summary>
    /// Число ячеек доски заказов задаёт уровень «Ямской избы»: три на первом, четвёртая со второго, пятая на пятом.
    /// </summary>
    /// <param name="postHouseLevel">Уровень «Ямской избы» игрока.</param>
    /// <param name="expectedSize">Сколько заказов держится на доске.</param>
    [TestCase(1, 3)]
    [TestCase(2, 4)]
    [TestCase(4, 4)]
    [TestCase(5, 5)]
    public void BoardSizeByPostHouseLevelTest(int postHouseLevel, int expectedSize)
    {
        var player = TestPlayer.Create()
            .WithPostHouseLevel(postHouseLevel);

        player.ClearOrders();

        Assert.That(player.Orders(), Has.Count.EqualTo(expectedSize));
    }

    /// <summary>
    /// С третьего уровня «Ямской избы» первая уступка за сутки не откладывает пополнение доски, а следующая – откладывает.
    /// </summary>
    [Test]
    public void FreeConcessionSkipsRefillDelayTest()
    {
        var player = TestPlayer.Create()
            .WithPostHouseLevel(OrderManager.FreeConcessionLevel);

        player.CancelOrder(player.Orders().First().Id);

        Assert.That(player.RefillAt(), Is.Null);

        player.CancelOrder(player.Orders().First().Id);

        Assert.That(player.RefillAt(), Is.Not.Null);
    }

    /// <summary>
    /// Ниже третьего уровня «Ямской избы» бесплатной уступки нет: первый же отказ откладывает пополнение доски.
    /// </summary>
    [Test]
    public void ConcessionDelaysRefillBelowFreeLevelTest()
    {
        var player = TestPlayer.Create()
            .WithPostHouseLevel(OrderManager.FreeConcessionLevel - 1);

        player.CancelOrder(player.Orders().First().Id);

        Assert.That(player.RefillAt(), Is.Not.Null);
    }

    /// <summary>
    /// С четвёртого уровня «Ямской избы» соседи держат два незавершённых поручения разом, ниже – только одно.
    /// </summary>
    /// <param name="postHouseLevel">Уровень «Ямской избы» игрока.</param>
    /// <param name="expectedErrands">Сколько незавершённых поручений остаётся у игрока.</param>
    [TestCase(3, 1)]
    [TestCase(4, 2)]
    public void SecondErrandOfferByPostHouseLevelTest(int postHouseLevel, int expectedErrands)
    {
        const int villageLevel = ErrandManager.ErrandUnlockLevel;

        var player = TestPlayer.Create()
            .WithPostHouseLevel(postHouseLevel);

        player.CreateOffer(villageLevel);
        player.CreateOffer(villageLevel);

        Assert.That(player.Errands(), Has.Length.EqualTo(expectedErrands));
    }
}

file static class PostHouseTestsActs
{
    public static TestPlayer WithPostHouseLevel(this TestPlayer p, int level)
    {
        using var scope = App.Scope();
        var domik = scope.Context.Domiks.Single(x => x.PlayerId == p.Id && x.TypeId == DomikManager.PostHouseTypeId);
        domik.Level = level;
        scope.Commit();
        return p;
    }

    public static int PostHouseLevel(this TestPlayer p)
    {
        using var scope = App.Scope();
        return scope.Context.Domiks.Single(x => x.PlayerId == p.Id && x.TypeId == DomikManager.PostHouseTypeId).Level;
    }

    public static TestPlayer ClearOrders(this TestPlayer p)
    {
        using var scope = App.Scope();
        var orders = scope.Context.Orders.Where(x => x.PlayerId == p.Id).ToArray();
        var orderIds = orders.Select(x => x.Id).ToArray();
        scope.Context.OrderResources.RemoveRange(scope.Context.OrderResources.Where(x => orderIds.Contains(x.OrderId)));
        scope.Context.Orders.RemoveRange(orders);
        scope.Commit();
        return p;
    }

    public static DateTime? RefillAt(this TestPlayer p)
    {
        using var scope = App.Scope();
        return scope.Context.Players.Single(x => x.Id == p.Id).NextOrderRefillAt;
    }

    public static TestPlayer CreateOffer(this TestPlayer p, int villageLevel)
    {
        App.Act<ErrandManager>(m => m.CreateOffer(p.Id, villageLevel));
        return p;
    }

    public static Domiki.Web.Economy.Models.Errand[] Errands(this TestPlayer p)
    {
        return App.Act<ErrandManager, Domiki.Web.Economy.Models.Errand[]>(m => m.GetAll(p.Id));
    }
}
