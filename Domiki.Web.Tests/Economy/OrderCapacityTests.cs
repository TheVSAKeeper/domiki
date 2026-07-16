using Domiki.Web.Economy;

namespace Domiki.Web.Tests;

public class OrderCapacityTests : TestBase
{
    private const int ClayResourceTypeId = 4;
    private const int StoneResourceTypeId = 2;

    /// <summary>
    /// Стартовая производственная мощность по глине (1 в сутки) ограничивает объём заказа для любого тира спроса.
    /// </summary>
    /// <param name="tierIndex">Индекс тира спроса.</param>
    /// <param name="expectedQuantity">Ожидаемое количество ресурса в заказе.</param>
    [TestCase(0, 2)]
    [TestCase(1, 5)]
    [TestCase(2, 16)]
    public void NewPlayerClayOrderQuantityIsCappedByStartingCapacityTest(int tierIndex, int expectedQuantity)
    {
        var playerId = GetPlayerId();
        var tier = OrderManager.Tiers[tierIndex];

        var capacity = GetCapacity(playerId, ClayResourceTypeId);
        var quantity = OrderManager.GetEffectiveQuantity(tier, ClayResourceTypeId, capacity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capacity, Is.EqualTo(1));
            Assert.That(quantity, Is.EqualTo(expectedQuantity));
        }
    }

    /// <summary>
    /// Ресурс, который игрок вообще не производит (нулевая мощность), всё равно даёт заказ не меньше чем на 2 единицы, для
    /// любого тира спроса.
    /// </summary>
    /// <param name="tierIndex">Индекс тира спроса.</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void PlayerWithoutMatchingBuildingFloorsAtTwoTest(int tierIndex)
    {
        var playerId = GetPlayerId();
        var tier = OrderManager.Tiers[tierIndex];

        var capacity = GetCapacity(playerId, StoneResourceTypeId);
        var quantity = OrderManager.GetEffectiveQuantity(tier, StoneResourceTypeId, capacity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capacity, Is.Zero);
            Assert.That(quantity, Is.EqualTo(2));
        }
    }

    private int GetPlayerId()
    {
        using var uow = GetUow();
        var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
        uow.Commit();
        return playerId;
    }

    private int GetCapacity(int playerId, int resourceTypeId)
    {
        using var uow = GetUow();
        var capacity = GetOrderManager(uow).GetCapacity(playerId, resourceTypeId);
        uow.Commit();
        return capacity;
    }
}
