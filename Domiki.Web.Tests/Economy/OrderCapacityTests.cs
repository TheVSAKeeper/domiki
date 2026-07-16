using Domiki.Web.Economy;

namespace Domiki.Web.Tests;

public sealed class OrderCapacityTests
{
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
        const int expectedCapacity = 1;

        var player = TestPlayer.Create();
        var tier = OrderManager.Tiers[tierIndex];

        var capacity = player.Capacity(ResourceIds.Clay);
        var quantity = OrderManager.GetEffectiveQuantity(tier, ResourceIds.Clay, capacity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capacity, Is.EqualTo(expectedCapacity));
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
        const int expectedQuantity = 2;

        var player = TestPlayer.Create();
        var tier = OrderManager.Tiers[tierIndex];

        var capacity = player.Capacity(ResourceIds.Stone);
        var quantity = OrderManager.GetEffectiveQuantity(tier, ResourceIds.Stone, capacity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capacity, Is.Zero);
            Assert.That(quantity, Is.EqualTo(expectedQuantity));
        }
    }
}
