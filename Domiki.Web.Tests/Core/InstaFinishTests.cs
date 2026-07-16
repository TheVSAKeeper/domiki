using Domiki.Web.Infrastructure;
using Domiki.Web.Workers;

namespace Domiki.Web.Tests;

public sealed class InstaFinishTests
{
    /// <summary>
    /// Ускорение улучшения домика в пределах лимита завершает улучшение немедленно и списывает золото.
    /// </summary>
    [Test]
    public void HurryDomikInCapFinishesAndWritesOffGoldTest()
    {
        var player = TestPlayer.Create();
        using (App.PendingEvents())
        {
            player.Buy(DomikIds.Market);
        }

        player.WithResource(ResourceIds.Gold, 3);
        SetDomikUpgradeFinish(player.Id, 3, DateTimeHelper.GetNowDate().AddHours(2));

        player.HurryDomik(3);

        var domik = player.Domiks().Single(x => x.Id == 3);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(domik.Level, Is.EqualTo(1));
            Assert.That(domik.FinishDate, Is.Null);
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Нельзя ускорить несуществующий домик или домик чужого игрока.
    /// </summary>
    [Test]
    public void HurryDomikMissingOrForeignThrowsTest()
    {
        var player = TestPlayer.Create();
        var otherPlayer = TestPlayer.Create();

        Assert.Throws<BusinessException>(() => player.HurryDomik(int.MaxValue));
        Assert.Throws<BusinessException>(() => otherPlayer.HurryDomik(StartingDomikIds.Barrack));
    }

    /// <summary>
    /// Нельзя ускорить домик, который сейчас не улучшается, – бросает ошибку «Домик не улучшается».
    /// </summary>
    [Test]
    public void HurryDomikNotUpgradingThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Assert.Throws<BusinessException>(() => player.HurryDomik(StartingDomikIds.Barrack));

        Assert.That(ex.Message, Is.EqualTo("Домик не улучшается"));
    }

    /// <summary>
    /// Ускорение производства в пределах лимита завершает его немедленно и списывает золото за сэкономленное время.
    /// </summary>
    [Test]
    public void HurryManufactureInCapFinishesAndWritesOffGoldTest()
    {
        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, 3);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddMinutes(40));

        player.HurryManufacture(manufactureId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(2));
            Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(1));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures, Is.Null.Or.Empty);
            Assert.That(player.WorkerList().Single().ManufactureId, Is.Null);
        }
    }

    /// <summary>
    /// Нельзя ускорить несуществующее производство или производство чужого игрока.
    /// </summary>
    [Test]
    public void HurryManufactureMissingOrForeignThrowsTest()
    {
        var player = CreatePlayerWithManufacture(out var manufactureId);
        var otherPlayer = TestPlayer.Create();

        Assert.Throws<BusinessException>(() => player.HurryManufacture(int.MaxValue));
        Assert.Throws<BusinessException>(() => otherPlayer.HurryManufacture(manufactureId));
    }

    /// <summary>
    /// Ускорение производства с оставшимся временем выше лимита (6 часов) запрещено и не меняет состояние производства.
    /// </summary>
    [Test]
    public void HurryManufactureOverCapThrowsAndKeepsStateTest()
    {
        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, 10);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddHours(6).AddSeconds(1));

        var ex = Assert.Throws<BusinessException>(() => player.HurryManufacture(manufactureId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("До конца ещё далеко"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(10));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }
    }

    /// <summary>
    /// Ускоренное завершение производства выдаёт ресурсы по проценту выхода, зафиксированному на момент старта, а не по
    /// стандартному.
    /// </summary>
    [Test]
    public void HurryManufactureUsesFixedOutputPercentTest()
    {
        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, 1);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddMinutes(10), 200);

        player.HurryManufacture(manufactureId);

        Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(2));
    }

    /// <summary>
    /// Ускорение производства при нехватке золота падает исключением и не списывает ресурсы и не завершает производство.
    /// </summary>
    [Test]
    public void HurryManufactureWithoutGoldThrowsAndKeepsStateTest()
    {
        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, 1);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddHours(2));

        var ex = Assert.Throws<BusinessException>(() => player.HurryManufacture(manufactureId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("Недостаточно Золото"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(1));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }
    }

    /// <summary>
    /// Стоимость ускорения производства в золоте округляется вверх по оставшемуся времени, а не считается дробно.
    /// </summary>
    /// <param name="remainingSeconds">Сколько секунд осталось до завершения.</param>
    /// <param name="expectedCost">Ожидаемая стоимость ускорения в золоте.</param>
    [TestCase(40 * 60, 1)]
    [TestCase(3 * 3600 + 60, 4)]
    [TestCase(6 * 3600, 6)]
    public void HurryManufactureCostCeilsRemainingTimeTest(int remainingSeconds, int expectedCost)
    {
        const int startGold = 6;

        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, startGold);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(remainingSeconds));

        player.HurryManufacture(manufactureId);

        Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold - expectedCost));
    }

    private static TestPlayer CreatePlayerWithManufacture(out int manufactureId)
    {
        var player = TestPlayer.Create();
        using (App.PendingEvents())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig);
        }

        manufactureId = player.Manufacture(StartingDomikIds.ClayMine).Id;
        return player;
    }

    private static void SetManufactureFinish(int manufactureId, DateTime finishDate, int? outputPercent = null)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.FinishDate = finishDate;
        if (outputPercent != null)
        {
            manufacture.OutputPercent = outputPercent.Value;
        }

        scope.Commit();
    }

    private static void SetDomikUpgradeFinish(int playerId, int domikId, DateTime finishDate)
    {
        using var scope = App.Scope();
        var domik = scope.Context.Domiks.Single(x => x.PlayerId == playerId && x.Id == domikId);
        Assert.That(domik.UpgradeSeconds, Is.Not.Null);
        domik.UpgradeCalculateDate = finishDate.AddSeconds(-domik.UpgradeSeconds!.Value);
        scope.Commit();
    }
}

file static class InstaFinishTestsActs
{
    public static IReadOnlyList<Domiki.Web.Workers.Models.Worker> WorkerList(this TestPlayer p)
    {
        return App.Act<WorkerManager, IReadOnlyList<Domiki.Web.Workers.Models.Worker>>(m => m.GetWorkers(p.Id).ToList());
    }
}
