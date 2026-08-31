using Domiki.Web.Activities.Models;
using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;

namespace Domiki.Web.Tests;

public sealed class ToolKeysTests
{
    /// <summary>
    /// Кайло куётся из трёх железа и доски, клещи – из двух железа и доски.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт ковки ключа.</param>
    /// <param name="ironValue">Количество железа на входе.</param>
    /// <param name="keyResourceTypeId">Тип выкованного ключа.</param>
    /// <param name="durationSeconds">Длительность ковки.</param>
    [TestCase(ReceiptIds.MakePick, 3, ResourceIds.Pick, 7200)]
    [TestCase(ReceiptIds.MakeTongs, 2, ResourceIds.Tongs, 3600)]
    public void KeyForgeReceiptsHaveExpectedInputsAndOutputsTest(int receiptId, int ironValue, int keyResourceTypeId, int durationSeconds)
    {
        var receipt = GetReceipt(receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(ResourceIds.Iron, ironValue), (ResourceIds.Board, 1)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(keyResourceTypeId, 1)]));
            Assert.That(receipt.DurationSeconds, Is.EqualTo(durationSeconds));
        }
    }

    /// <summary>
    /// Артельная смена длится двадцать часов и укладывается в календарные сутки вместе с отдыхом: выход 160 на пять рук
    /// даёт канонные 1,6 ресурса на трудяго-час, а монетный вход держит ставку в две монеты за трудяго-час.
    /// </summary>
    /// <param name="receiptId">Проверяемый артельный рецепт.</param>
    [TestCase(ReceiptIds.ClayDigArtel)]
    [TestCase(ReceiptIds.StoneDigArtel)]
    [TestCase(ReceiptIds.OreDigArtel)]
    public void ArtelShiftFitsIntoDayTest(int receiptId)
    {
        const int expectedHours = 20;
        const double canonYieldPerHour = 1.6;
        const double coinRatePerHour = 2.0;

        var receipt = GetReceipt(receiptId);
        var plodderHours = (double)receipt.PlodderCount * expectedHours;
        var coins = receipt.InputResources.Single(x => x.Type.Id == ResourceIds.Coin);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.DurationSeconds, Is.EqualTo(expectedHours * 3600));
            Assert.That(receipt.OutputResources.Single().Value / plodderHours, Is.EqualTo(canonYieldPerHour).Within(0.001));
            Assert.That(coins.Value / plodderHours, Is.EqualTo(coinRatePerHour).Within(0.001));
        }
    }

    /// <summary>
    /// Рецепты под ключ тратят ключ обязательным входом и отдают крупную партию за одну смену.
    /// </summary>
    /// <param name="receiptId">Проверяемый рецепт под ключ.</param>
    /// <param name="inputResourceTypeId">Тип основного входного ресурса.</param>
    /// <param name="inputValue">Количество основного входного ресурса.</param>
    /// <param name="keyResourceTypeId">Тип ключа, который тратит рецепт.</param>
    /// <param name="outputResourceTypeId">Тип выходного ресурса.</param>
    /// <param name="outputValue">Количество выходного ресурса.</param>
    /// <param name="plodderCount">Сколько трудяг занимает смена.</param>
    [TestCase(ReceiptIds.ClayDigArtel, ResourceIds.Coin, 200, ResourceIds.Pick, ResourceIds.Clay, 160, 5)]
    [TestCase(ReceiptIds.StoneDigArtel, ResourceIds.Coin, 200, ResourceIds.Pick, ResourceIds.Stone, 160, 5)]
    [TestCase(ReceiptIds.OreDigArtel, ResourceIds.Coin, 200, ResourceIds.Pick, ResourceIds.Ore, 160, 5)]
    [TestCase(ReceiptIds.SplitBlock, ResourceIds.Stone, 48, ResourceIds.Pick, ResourceIds.Block, 30, 1)]
    [TestCase(ReceiptIds.BigKiln, ResourceIds.Clay, 48, ResourceIds.Tongs, ResourceIds.Brick, 28, 1)]
    [TestCase(ReceiptIds.BlastFurnace, ResourceIds.Ore, 48, ResourceIds.Tongs, ResourceIds.Iron, 28, 1)]
    public void KeyReceiptsSpendKeyAndYieldBatchTest(int receiptId, int inputResourceTypeId, int inputValue, int keyResourceTypeId, int outputResourceTypeId, int outputValue, int plodderCount)
    {
        var receipt = GetReceipt(receiptId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.InputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(inputResourceTypeId, inputValue), (keyResourceTypeId, 1)]));
            Assert.That(receipt.OutputResources.Select(x => (x.Type.Id, x.Value)), Is.EquivalentTo([(outputResourceTypeId, outputValue)]));
            Assert.That(receipt.OptionalInputResources, Is.Empty);
            Assert.That(receipt.PlodderCount, Is.EqualTo(plodderCount));
        }
    }

    /// <summary>
    /// Рецепты ключей привязаны к уровню постройки и на уровень ниже не открыты.
    /// </summary>
    /// <param name="domikTypeId">Тип постройки.</param>
    /// <param name="level">Уровень постройки.</param>
    /// <param name="receiptId">Проверяемый рецепт.</param>
    /// <param name="expected">Ожидается ли привязка.</param>
    [TestCase(DomikIds.Forge, 3, ReceiptIds.MakePick, true)]
    [TestCase(DomikIds.Forge, 2, ReceiptIds.MakePick, false)]
    [TestCase(DomikIds.Forge, 4, ReceiptIds.MakeTongs, true)]
    [TestCase(DomikIds.Forge, 3, ReceiptIds.MakeTongs, false)]
    [TestCase(DomikIds.Forge, 4, ReceiptIds.BlastFurnace, true)]
    [TestCase(DomikIds.ClayMine, 3, ReceiptIds.ClayDigArtel, true)]
    [TestCase(DomikIds.ClayMine, 2, ReceiptIds.ClayDigArtel, false)]
    [TestCase(DomikIds.StoneMine, 3, ReceiptIds.StoneDigArtel, true)]
    [TestCase(DomikIds.GoldMine, 3, ReceiptIds.OreDigArtel, true)]
    [TestCase(DomikIds.Stonecutter, 3, ReceiptIds.SplitBlock, true)]
    [TestCase(DomikIds.Pottery, 4, ReceiptIds.BigKiln, true)]
    [TestCase(DomikIds.Pottery, 3, ReceiptIds.BigKiln, false)]
    public void KeyReceiptBindingsTest(int domikTypeId, int level, int receiptId, bool expected)
    {
        var domikType = App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes()).Single(x => x.Id == domikTypeId);
        var receiptIds = domikType.Levels.Single(x => x.Value == level).Receipts.Select(x => x.Id);

        Assert.That(receiptIds.Contains(receiptId), Is.EqualTo(expected));
    }

    /// <summary>
    /// Ковку ключа открывает чертёж соседа, а не постройка: кайло даёт Каменка на 40 репутации, клещи – Глинищи на 50.
    /// </summary>
    /// <param name="blueprintId">Проверяемый чертёж.</param>
    /// <param name="receiptId">Рецепт, который открывает чертёж.</param>
    /// <param name="neighborId">Сосед-источник чертежа.</param>
    /// <param name="threshold">Порог репутации.</param>
    [TestCase(BlueprintIds.Pick, ReceiptIds.MakePick, NeighborIds.Kamenka, 40)]
    [TestCase(BlueprintIds.Tongs, ReceiptIds.MakeTongs, NeighborIds.Glinischi, 50)]
    public void KeyBlueprintOpensReceiptTest(int blueprintId, int receiptId, int neighborId, int threshold)
    {
        var blueprint = App.Act<ResourceManager, Blueprint[]>(m => m.GetBlueprints()).Single(x => x.Id == blueprintId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blueprint.ReceiptId, Is.EqualTo(receiptId));
            Assert.That(blueprint.DomikTypeId, Is.Null);
            Assert.That(blueprint.NeighborId, Is.EqualTo(neighborId));
            Assert.That(blueprint.ReputationThreshold, Is.EqualTo(threshold));
        }
    }

    /// <summary>
    /// Без чертежа ковка кайла не начинается, даже когда постройка нужного уровня и припасы на месте.
    /// </summary>
    [Test]
    public void ForgingKeyWithoutBlueprintIsRejectedTest()
    {
        const int forgeDomikId = 3;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Forge, 3)
            .WithResource(ResourceIds.Iron, 3)
            .WithResource(ResourceIds.Board, 1);

        Assert.Throws<BusinessException>(() => player.StartManufacture(forgeDomikId, ReceiptIds.MakePick));
    }

    /// <summary>
    /// С репутацией у Каменки чертёж выдаётся сам, и ковка кайла списывает железо с досками.
    /// </summary>
    [Test]
    public void ForgingKeyWithReputationSpendsInputsTest()
    {
        const int forgeDomikId = 3;
        const int startIron = 3;
        const int startBoard = 1;

        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Forge, 3)
            .WithResource(ResourceIds.Iron, startIron)
            .WithResource(ResourceIds.Board, startBoard)
            .WithReputation(NeighborIds.Kamenka, 40);

        player.StartManufacture(forgeDomikId, ReceiptIds.MakePick);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(player.Resource(ResourceIds.Iron), Is.EqualTo(startIron - 3));
            Assert.That(player.Resource(ResourceIds.Board), Is.EqualTo(startBoard - 1));
        }
    }

    /// <summary>
    /// Чертёж целится ровно в одно: постройку или рецепт, и один рецепт открывает не больше одного чертежа.
    /// </summary>
    /// <remarks>
    /// Гейт в <see cref="Domiki.Web.Core.DomikManager"/> и замок карточки на фронте берут первый совпавший чертёж,
    /// поэтому второй чертёж на тот же рецепт молча остался бы без действия; в схеме это держит уникальный индекс.
    /// </remarks>
    [Test]
    public void BlueprintTargetsAreDistinctTest()
    {
        var blueprints = App.Act<ResourceManager, Blueprint[]>(m => m.GetBlueprints());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blueprints.Where(x => (x.DomikTypeId == null) == (x.ReceiptId == null)), Is.Empty);
            Assert.That(blueprints.Where(x => x.ReceiptId != null).GroupBy(x => x.ReceiptId).Where(x => x.Count() > 1), Is.Empty);
        }
    }

    private static Receipt GetReceipt(int receiptId)
    {
        return App.Act<ResourceManager, Receipt[]>(m => m.GetReceipts()).Single(x => x.Id == receiptId);
    }
}
