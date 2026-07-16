namespace Domiki.Web.Tests;

public sealed class SoftlockInvariantTests
{
    /// <summary>
    /// Игрок без монет не попадает в софтлок: копка глины не требует монет, а сдача заказа на глину возвращает монеты в
    /// оборот.
    /// </summary>
    [Test]
    public void PlayerWithoutCoinsCanDigClayAndCompleteAffordableOrderForCoinsTest()
    {
        var player = TestPlayer.Create();
        SetResourceValue(player.Id, ResourceIds.Coin, 0);

        Assert.DoesNotThrow(() => player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig));
        Assert.That(player.Resource(ResourceIds.Clay), Is.GreaterThan(0));

        var order = player.Orders().First(x => x.Resources.Single().Type.Id == ResourceIds.Clay);
        var need = order.Resources.Single();
        SetResourceValue(player.Id, ResourceIds.Coin, 0);
        EnsureResourceAtLeast(player.Id, need.Type.Id, need.Value);

        player.CompleteOrder(order.Id);

        Assert.That(player.Resource(ResourceIds.Coin), Is.GreaterThan(0));
    }

    private static void SetResourceValue(int playerId, int typeId, int value)
    {
        using var scope = App.Scope();
        var resource = scope.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId);
        resource.Value = value;
        scope.Commit();
    }

    private static void EnsureResourceAtLeast(int playerId, int typeId, int value)
    {
        using var scope = App.Scope();
        var resource = scope.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId);
        resource.Value = Math.Max(resource.Value, value);
        scope.Commit();
    }
}
