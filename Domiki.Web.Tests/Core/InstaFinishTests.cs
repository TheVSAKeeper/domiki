using System.Text.Json;
using Domiki.Web.Core;
using Domiki.Web.Infrastructure;

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
    /// Нельзя ускорить домик, который сейчас не улучшается, – бросает ошибку «Улучшение уже закончилось».
    /// </summary>
    [Test]
    public void HurryDomikNotUpgradingThrowsTest()
    {
        var player = TestPlayer.Create();

        var ex = Throws.Business(() => player.HurryDomik(StartingDomikIds.Barrack));

        Assert.That(ex.Message, Is.EqualTo("Улучшение уже закончилось"));
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
            Assert.That(player.Workers().Single().ManufactureId, Is.Null);
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
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddHours(6).AddMinutes(5));

        var ex = Throws.Business(() => player.HurryManufacture(manufactureId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("До конца ещё далеко – поторопить можно только в последние 6 ч"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(10));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }
    }

    /// <summary>
    /// Производство, которому до конца меньше 15 минут, поторопить нельзя: отказ не списывает золото и не завершает
    /// производство.
    /// </summary>
    /// <param name="remainingSeconds">Сколько секунд осталось до завершения.</param>
    [TestCase(14 * 60)]
    [TestCase(5 * 60)]
    [TestCase(60)]
    public void HurryManufactureBelowWindowThrowsTest(int remainingSeconds)
    {
        const int startGold = 10;

        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, startGold);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(remainingSeconds));

        var ex = Throws.Business(() => player.HurryManufacture(manufactureId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo("До конца меньше 15 минут – дождись, золото тут ни к чему"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold));
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
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddMinutes(20), 200);

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

        var ex = Throws.Business(() => player.HurryManufacture(manufactureId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Does.StartWith("Не хватает: Золото ×"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(1));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }
    }

    /// <summary>
    /// Стоимость ускорения производства – золотой за трудяго-час оставшейся работы с округлением вверх по всей смене:
    /// толпа из 5 трудяг за час до конца платит 5, за полчаса – 3, артель за 6 ч – 30.
    /// </summary>
    /// <param name="remainingSeconds">Сколько секунд осталось до завершения.</param>
    /// <param name="plodderCount">Сколько трудяг занято сменой.</param>
    /// <param name="expectedCost">Ожидаемая стоимость ускорения в золоте.</param>
    [TestCase(3600, 1, 1)]
    [TestCase(1200, 1, 1)]
    [TestCase(3600, 5, 5)]
    [TestCase(1800, 5, 3)]
    [TestCase(901, 5, 2)]
    [TestCase(21600, 5, 30)]
    [TestCase(4320, 5, 6)]
    [TestCase(21600, 1, 6)]
    public void HurryManufactureCostCeilsRemainingTimeTest(int remainingSeconds, int plodderCount, int expectedCost)
    {
        const int startGold = 30;

        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, startGold);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(remainingSeconds), plodderCount: plodderCount);

        player.HurryManufacture(manufactureId, confirmed: true);

        Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold - expectedCost));
    }

    /// <summary>
    /// Смену дороже 6 золотых сервер торопит только с подтверждением: без него отказ не списывает золото и не завершает
    /// смену, с ним списывается полная цена; ровно 6 золотых проходят без подтверждения. Толпа из 5 трудяг за 72 мин до
    /// конца платит 6, за 73 мин – 7.
    /// </summary>
    /// <param name="remainingSeconds">Сколько секунд осталось до завершения.</param>
    /// <param name="confirmed">Подтвердил ли игрок цену.</param>
    /// <param name="allowed">Торопится ли смена.</param>
    /// <param name="cost">Цена ускорения в золоте.</param>
    [TestCase(4320, false, true, 6)]
    [TestCase(4380, false, false, 7)]
    [TestCase(4380, true, true, 7)]
    public void HurryManufactureConfirmThresholdTest(int remainingSeconds, bool confirmed, bool allowed, int cost)
    {
        const int startGold = 30;
        const int plodderCount = 5;

        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, startGold);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(remainingSeconds), plodderCount: plodderCount);

        if (allowed)
        {
            player.HurryManufacture(manufactureId, confirmed);

            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold - cost));
            return;
        }

        var ex = Throws.Business(() => player.HurryManufacture(manufactureId, confirmed));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.Message, Is.EqualTo($"Поторопить дорого, золото ×{cost} – обнови страницу и подтверди цену"));
            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold));
            Assert.That(player.Domiks().Single(x => x.Id == StartingDomikIds.ClayMine).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }
    }

    /// <summary>
    /// Команда пачки «Поторопить» без поля confirmed – намерение старого клиента или из офлайн-очереди – считается
    /// неподтверждённой: смену за 7 золотых она не торопит, а с confirmed: true торопит.
    /// </summary>
    /// <param name="confirmed">Значение поля confirmed; <see langword="null"/> – поля в команде нет.</param>
    /// <param name="allowed">Торопится ли смена.</param>
    [TestCase(null, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void HurryManufactureCommandConfirmedFieldTest(bool? confirmed, bool allowed)
    {
        const int startGold = 30;
        const int plodderCount = 5;
        const int cost = 7;

        var player = CreatePlayerWithManufacture(out var manufactureId);
        player.WithResource(ResourceIds.Gold, startGold);
        SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(4380), plodderCount: plodderCount);

        object args = confirmed == null ? new { manufactureId } : new { manufactureId, confirmed };
        var hurry = () => player.HurryManufactureCommand(args);

        if (allowed)
        {
            hurry();

            Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold - cost));
            return;
        }

        Throws.Business(hurry);

        Assert.That(player.Resource(ResourceIds.Gold), Is.EqualTo(startGold));
    }

    /// <summary>
    /// Окно ускорения включает обе границы: ровно 15 минут и ровно 6 часов до конца торопить можно, на секунду за ними –
    /// нельзя.
    /// </summary>
    /// <param name="remainingSeconds">Сколько секунд осталось до завершения.</param>
    /// <param name="allowed">Можно ли поторопить.</param>
    [TestCase(15 * 60, true)]
    [TestCase(15 * 60 - 1, false)]
    [TestCase(6 * 3600, true)]
    [TestCase(6 * 3600 + 1, false)]
    public void InstaFinishWindowBoundsTest(int remainingSeconds, bool allowed)
    {
        var now = DateTimeHelper.GetNowDate();

        var cost = () => DomikManager.GetInstaFinishCost(now.AddSeconds(remainingSeconds), now, 1);

        if (allowed)
        {
            Assert.That(cost(), Is.Positive);
        }
        else
        {
            Assert.Throws<BusinessException>(() => cost());
        }
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

    private static void SetManufactureFinish(int manufactureId, DateTime finishDate, int? outputPercent = null, int? plodderCount = null)
    {
        using var scope = App.Scope();
        var manufacture = scope.Context.Manufactures.Single(x => x.Id == manufactureId);
        manufacture.FinishDate = finishDate;
        if (outputPercent != null)
        {
            manufacture.OutputPercent = outputPercent.Value;
        }

        if (plodderCount != null)
        {
            manufacture.PlodderCount = plodderCount.Value;
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
    public static void HurryManufactureCommand(this TestPlayer p, object args) =>
        App.Act<GameCommandRegistry>(r => r.Execute(p.Id, "HurryManufacture", JsonSerializer.SerializeToElement(args)));
}
