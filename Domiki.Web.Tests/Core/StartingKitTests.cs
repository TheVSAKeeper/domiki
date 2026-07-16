using Domiki.Web.Core;

namespace Domiki.Web.Tests;

public sealed class StartingKitTests
{
    /// <summary>
    /// Стартовый комплект построек виден сразу же в той же транзакции, где только что был создан игрок, без перечитывания из
    /// БД.
    /// </summary>
    [Test]
    public void GetDomiksInSameScopeAsGetPlayerIdSeesStartingKitTest()
    {
        using var scope = App.Scope();
        var domikManager = scope.Get<DomikManager>();
        var playerId = domikManager.GetPlayerId($"testUser_{App.RunId}_{Guid.NewGuid()}");

        var domiks = domikManager.GetDomiks(playerId);

        Assert.That(domiks.Count(), Is.EqualTo(2));
        scope.Commit();
    }

    /// <summary>
    /// Стартовый комплект построек и рабочих даёт новому игроку обжитость деревни, равную 4.
    /// </summary>
    [Test]
    public void NewPlayerStartingVillageLevelIsFourTest()
    {
        var player = TestPlayer.Create();

        var level = player.GetVillageLevel();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(level.Buildings, Is.EqualTo(2));
            Assert.That(level.Residents, Is.EqualTo(1));
            Assert.That(level.Level, Is.EqualTo(4));
        }
    }

    /// <summary>
    /// Новый игрок сразу получает барак и глиняный карьер первого уровня и стартовый запас монет.
    /// </summary>
    [Test]
    public void NewPlayerStartsWithBarrackAndClayMineAndCoinsTest()
    {
        var player = TestPlayer.Create();

        var domiks = player.Domiks().OrderBy(x => x.Id).ToArray();
        Assert.That(domiks.Length, Is.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(domiks[0].Type.Id, Is.EqualTo(DomikIds.Barrack));
            Assert.That(domiks[0].Level, Is.EqualTo(1));
            Assert.That(domiks[1].Type.Id, Is.EqualTo(DomikIds.ClayMine));
            Assert.That(domiks[1].Level, Is.EqualTo(1));
        }

        Assert.That(player.Resource(ResourceIds.Coin), Is.EqualTo(DomikManager.StartingCoins));
    }

    /// <summary>
    /// Даже без единой монеты новый игрок может запустить копку глины на стартовом карьере, ничего не покупая.
    /// </summary>
    [Test]
    public void NewPlayerWithZeroCoinsCanStartClayDigWithoutBuyingAnythingTest()
    {
        var player = TestPlayer.Create();
        var coins = player.Resource(ResourceIds.Coin);
        player.WithResource(ResourceIds.Coin, -coins);
        Assert.That(player.Resource(ResourceIds.Coin), Is.Zero);

        Assert.DoesNotThrow(() => player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig));
    }
}
