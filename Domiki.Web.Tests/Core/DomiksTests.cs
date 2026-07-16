using Domiki.Web.Core;
using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

public class DomiksTests
{
    private const int ClearWeatherTypeId = 1;
    private const int MarketTypeId = 7;

    [SetUp]
    public void SetUp()
    {
        SetWeather(ClearWeatherTypeId);
    }

    [TearDown]
    public void TearDown()
    {
        ClearWeatherSchedule();
    }

    /// <summary>
    /// Покупаем домик и проверяем, что у нас 1 домик первого уровня.
    /// </summary>
    [Test]
    public void BuyDomikTest()
    {
        var player = TestPlayer.Create().WithDomik(MarketTypeId);
        var beforeResources = player.Resources();
        var types = player.DomikTypes();
        var buyType = types.First(x => x.UnlockLevel == 0);
        player.Buy(buyType.Id);

        var afterResources = player.Resources();
        var domiks = player.Domiks();
        var domiksCount = domiks.Count();
        Assert.That(domiksCount, Is.EqualTo(4));
        var level = domiks.First().Level;
        Assert.That(level, Is.EqualTo(1));

        foreach (var resource in buyType.Levels[0].Resources)
        {
            var beforeResource = beforeResources.First(x => x.Type.Id == resource.Type.Id);
            var afterResource = afterResources.First(x => x.Type.Id == resource.Type.Id);
            var actualDiffResource = beforeResource.Value - afterResource.Value;
            var expectedDiffResource = resource.Value;
            Assert.That(actualDiffResource, Is.EqualTo(expectedDiffResource));
        }
    }

    /// <summary>
    /// Новый игрок стартует ровно с одним видом ресурса – монетами в размере стартового капитала.
    /// </summary>
    [Test]
    public void CheckBaseResourcesTest()
    {
        var player = TestPlayer.Create();
        var resources = player.Resources();
        Assert.That(resources.Count, Is.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.First().Type.Id, Is.EqualTo(1));
            Assert.That(resources.First().Value, Is.EqualTo(DomikManager.StartingCoins));
        }
    }

    /// <summary>
    /// Проверка на то, что конкурирующие запросы не могут превысить лимит.
    /// </summary>
    [Test]
    public void ConcurrencyBuyDomikTest()
    {
        for (var i = 1; i <= 217; i++)
        {
            var player = TestPlayer.Create();
            var types = player.DomikTypes();
            var domikType = types.First(x => x.UnlockLevel == 0 && x.MaxCount == 1);
            Assert.That(domikType.MaxCount, Is.EqualTo(1));
            var domikTypeId = domikType.Id;

            var numbers = Enumerable.Range(0, 10).ToList();
            Parallel.ForEach(numbers, number =>
            {
                try
                {
                    player.Buy(domikTypeId);
                }
                catch (Exception ex)
                {
                }
            });

            var domiks = player.Domiks();
            var domiksCount = domiks.Count();
            Assert.That(domiksCount, Is.EqualTo(3), "iterarion number " + i);
        }
    }

    /// <summary>
    /// Проверка на то, что конкурирующие запросы корректно улучшают домик.
    /// </summary>
    [Test]
    public void ConcurrencyUpgradeDomikTest()
    {
        for (var i = 1; i <= 217; i++)
        {
            var player = TestPlayer.Create();
            var types = player.DomikTypes();
            var domikType = types.First(x => x.UnlockLevel == 0 && x.MaxCount == 1);
            Assert.That(domikType.MaxCount, Is.EqualTo(1));
            var domikTypeId = domikType.Id;
            player.Buy(domikTypeId);

            var actionCount = 4;
            var numbers = Enumerable.Range(0, actionCount).ToList();
            var errorCount = 0;
            Parallel.ForEach(numbers, number =>
            {
                try
                {
                    player.Upgrade(1);
                }
                catch (Exception ex)
                {
                    errorCount++;
                }
            });

            var domiks = player.Domiks();
            var level = domiks.First().Level;
            var checkValue = level + errorCount;
            var expected = 1 + actionCount;

            // минимум один будет успешный
            Assert.That(checkValue, Is.GreaterThan(1));

            // количество успешных улучшений + 1 базовый уровень + количество ошибок равно = 1 базовый уровень + количество действий
            Assert.That(checkValue, Is.EqualTo(expected), "iterarion number " + i + ", checkValue " + checkValue + ",  error count " + errorCount);
        }
    }

    /// <summary>
    /// Первое обращение по внешнему идентификатору заводит игрока и возвращает положительный id.
    /// </summary>
    [Test]
    public void GetPlayerIdTest()
    {
        var player = TestPlayer.Create();
        Assert.Greater(player.Id, 0);
    }

    /// <summary>
    /// Полный состав рабочих в групповом рецепте даёт премиальный выход ресурса, но требует доплаты монетами.
    /// </summary>
    [Test]
    public void GroupRecipeOutputPremiumTest()
    {
        var player = TestPlayer.Create().WithResource(1, 100);
        var barakTypeId = 2;
        var clayMineTypeId = 5;
        var groupReceiptId = 2;
        var coinResourceTypeId = 1;
        var clayResourceTypeId = 4;
        player.WithDomiks(barakTypeId, 4).WithDomik(clayMineTypeId).Upgrade(7);
        var beforeCoin = player.Resource(coinResourceTypeId);
        player.StartManufacture(7, groupReceiptId);
        var after = player.Resources();
        var clay = after.First(x => x.Type.Id == clayResourceTypeId).Value;
        var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clay, Is.EqualTo(8));
            Assert.That(coinDiff, Is.EqualTo(-5));
        }
    }

    /// <summary>
    /// Домик нулевого уровня (ещё строящийся) не мешает вести производство на других постройках игрока.
    /// </summary>
    [Test]
    public void LevelZeroDomikDoesNotBreakManufactureTest()
    {
        var player = TestPlayer.Create();
        var clayMineTypeId = 5;
        var clayDigReceiptId = 1;
        var clayResourceTypeId = 4;
        var startingClayMineId = 2;
        player.WithDomik(clayMineTypeId, 0);
        Assert.DoesNotThrow(() => player.StartManufacture(startingClayMineId, clayDigReceiptId));
        var clay = player.Resource(clayResourceTypeId);
        Assert.That(clay, Is.EqualTo(1));
    }

    /// <summary>
    /// Добудем одну глину в глиняном карьере.
    /// </summary>
    [Test]
    public void ManufactureTest()
    {
        var player = TestPlayer.Create();
        var clayMineId = 5;
        var barakTypeId = 2;
        player.WithDomik(clayMineId).WithDomik(barakTypeId);
        var coinResourceTypeId = 1;
        var clayResourceTypeId = 4;
        var clayDigReceiptId = 1;
        var beforeCointResourceValue = player.Resource(coinResourceTypeId);
        player.StartManufacture(3, clayDigReceiptId);
        var afterResources = player.Resources();
        var afterClayResourceValue = afterResources.First(x => x.Type.Id == clayResourceTypeId).Value;
        var afterCointResourceValue = afterResources.First(x => x.Type.Id == coinResourceTypeId).Value;
        var coinDiff = afterCointResourceValue - beforeCointResourceValue;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterClayResourceValue, Is.EqualTo(1));
            Assert.That(coinDiff, Is.Zero);
        }
    }

    /// <summary>
    /// Один барак даёт место одному рабочему, нельзя запустить два производства с одним рабочим.
    /// </summary>
    [Test]
    public void MaxWorkerCountTest()
    {
        var player = TestPlayer.Create();
        var clayMineTypeId = 5;
        player.WithDomik(clayMineTypeId);
        var clayDigReceiptId = 1;
        var startingClayMineId = 2;
        var boughtClayMineId = 3;
        using (TestCalculator.Defer())
        {
            player.StartManufacture(boughtClayMineId, clayDigReceiptId);
            Assert.Throws<BusinessException>(() => player.StartManufacture(startingClayMineId, clayDigReceiptId), "Exception not throw");
        }
    }

    /// <summary>
    /// Если у рецепта нет опционального слота, флаг использования инструмента игнорируется, и инструмент не расходуется.
    /// </summary>
    [Test]
    public void OptionalToolIgnoredWhenReceiptHasNoOptionalTest()
    {
        var player = TestPlayer.Create();
        var clayResourceTypeId = 4;
        var toolResourceTypeId = 8;
        var clayDigReceiptId = 1;
        player.WithResource(toolResourceTypeId, 3);

        player.StartManufacture(2, clayDigReceiptId, true);

        var resources = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.First(x => x.Type.Id == clayResourceTypeId).Value, Is.EqualTo(1));
            Assert.That(resources.First(x => x.Type.Id == toolResourceTypeId).Value, Is.EqualTo(3));
        }
    }

    /// <summary>
    /// Нельзя запросить опциональный инструмент в производстве, если инструмента нет на складе – бросает исключение.
    /// </summary>
    [Test]
    public void OptionalToolRequiresToolTest()
    {
        var player = TestPlayer.Create();
        var clayDig8hReceiptId = 14;

        Assert.Throws<BusinessException>(() => player.StartManufacture(2, clayDig8hReceiptId, true));
    }

    /// <summary>
    /// Полная гончарная цепочка: добытая глина превращается в посуду, а её продажа на рынке приносит монеты.
    /// </summary>
    [Test]
    public void PotteryDishesAndSellTest()
    {
        var player = TestPlayer.Create().WithResource(1, 700);
        var potteryBlueprintId = 3;
        player.WithBlueprint(potteryBlueprintId);
        var barakTypeId = 2;
        var clayMineTypeId = 5;
        var potteryTypeId = 13;
        var marketTypeId = 7;
        var coinResourceTypeId = 1;
        var clayResourceTypeId = 4;
        var dishesResourceTypeId = 12;
        var clayDig8hReceiptId = 14;
        var makeDishesReceiptId = 43;
        var sellDishesReceiptId = 45;
        player.WithDomik(barakTypeId).WithDomik(clayMineTypeId).WithDomik(barakTypeId).WithDomik(barakTypeId).Buy(potteryTypeId).Buy(marketTypeId);
        player.StartManufacture(4, clayDig8hReceiptId);
        player.StartManufacture(7, makeDishesReceiptId);
        var afterDishes = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterDishes.First(x => x.Type.Id == dishesResourceTypeId).Value, Is.EqualTo(1));
            Assert.That(afterDishes.First(x => x.Type.Id == clayResourceTypeId).Value, Is.EqualTo(6));
        }

        var beforeSellCoin = afterDishes.First(x => x.Type.Id == coinResourceTypeId).Value;
        player.StartManufacture(8, sellDishesReceiptId);
        var afterSell = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterSell.First(x => x.Type.Id == dishesResourceTypeId).Value, Is.Zero);
            Assert.That(afterSell.First(x => x.Type.Id == coinResourceTypeId).Value - beforeSellCoin, Is.EqualTo(45));
        }
    }

    /// <summary>
    /// Нельзя запустить производство на домике, который ещё строится (уровень 0), – прилетает ошибка «Домик ещё строится».
    /// </summary>
    [Test]
    public void StartManufactureOnUnbuiltDomikThrowsTest()
    {
        var player = TestPlayer.Create();
        var clayMineTypeId = 5;
        var clayDigReceiptId = 1;
        player.WithDomik(clayMineTypeId, 0);
        var ex = Assert.Throws<BusinessException>(() => player.StartManufacture(3, clayDigReceiptId));
        Assert.That(ex.Message, Is.EqualTo("Домик ещё строится"));
    }

    /// <summary>
    /// Запуск производства, не требующего монет по рецепту, не падает даже при нулевом балансе монет.
    /// </summary>
    [Test]
    public void StartManufactureWithZeroCoinsDoesNotThrowTest()
    {
        var player = TestPlayer.Create();
        var clayMineTypeId = 5;
        var barakTypeId = 2;
        var coinResourceTypeId = 1;
        var clayDigReceiptId = 1;
        player.WithDomik(clayMineTypeId).WithDomik(barakTypeId);

        var coins = player.Resource(coinResourceTypeId);
        player.WithResource(coinResourceTypeId, -coins);
        Assert.That(player.Resource(coinResourceTypeId), Is.Zero);

        Assert.DoesNotThrow(() => player.StartManufacture(3, clayDigReceiptId));
    }

    /// <summary>
    /// Улучшение домика поднимает его уровень и списывает ресурсы по стоимости достигнутого уровня.
    /// </summary>
    [Test]
    public void UpgradeDomikTest()
    {
        var player = TestPlayer.Create();
        var types = player.DomikTypes();
        var buyType = types.First(x => x.UnlockLevel == 0);
        var beforeResources = player.Resources();
        player.Upgrade(1);

        var domiks = player.Domiks();
        var level = domiks.First().Level;
        Assert.That(level, Is.EqualTo(2));

        var afterResources = player.Resources();

        foreach (var resource in buyType.Levels.First(x => x.Value == 2).Resources)
        {
            var beforeResource = beforeResources.First(x => x.Type.Id == resource.Type.Id);
            var afterResource = afterResources.First(x => x.Type.Id == resource.Type.Id);
            var actualDiffResource = beforeResource.Value - afterResource.Value;
            var expectedDiffResource = resource.Value;
            Assert.That(actualDiffResource, Is.EqualTo(expectedDiffResource));
        }
    }

    /// <summary>
    /// Улучшение до следующего уровня требует не только монет, но и материалов – без них апгрейд падает исключением.
    /// </summary>
    [Test]
    public void UpgradeToLevel3RequiresMaterialsTest()
    {
        var player = TestPlayer.Create().WithResource(1, 70);
        player.WithDomik(2).WithDomik(2).Upgrade(3);
        Assert.Throws<BusinessException>(() => player.Upgrade(3));
    }

    /// <summary>
    /// Копка в карьере выдаёт именно тот ресурс, что привязан к рецепту месторождения, и не тратит монеты.
    /// </summary>
    /// <param name="mineTypeId">Тип карьера.</param>
    /// <param name="receiptId">Рецепт копки.</param>
    /// <param name="outResourceTypeId">Ожидаемый добываемый ресурс.</param>
    [TestCase(3, 4, 2)]
    [TestCase(6, 5, 3)]
    public void DigProducesCorrectResourceTest(int mineTypeId, int receiptId, int outResourceTypeId)
    {
        var player = TestPlayer.Create();
        var barakTypeId = 2;
        var coinResourceTypeId = 1;
        player.WithDomik(barakTypeId).WithDomik(barakTypeId).WithDomik(mineTypeId);
        var beforeCoin = player.Resource(coinResourceTypeId);
        player.StartManufacture(5, receiptId);
        var after = player.Resources();
        var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
        var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(outValue, Is.EqualTo(1));
            Assert.That(coinDiff, Is.Zero);
        }
    }

    /// <summary>
    /// Групповой рецепт требует полного числа занятых рабочих мест: при нехватке бараков запуск производства падает
    /// исключением.
    /// </summary>
    /// <param name="barakCount">Количество бараков (занятых рабочих мест).</param>
    /// <param name="expectThrow">Ожидается ли исключение при запуске.</param>
    [TestCase(5, false)]
    [TestCase(4, true)]
    public void GroupRecipePlodderCountHonoredTest(int barakCount, bool expectThrow)
    {
        var player = TestPlayer.Create().WithResource(1, 100);
        var barakTypeId = 2;
        var clayMineTypeId = 5;
        var groupReceiptId = 2;
        var additionalBarakCount = barakCount - 1;
        player.WithDomiks(barakTypeId, additionalBarakCount).WithDomik(clayMineTypeId);
        var clayMineId = additionalBarakCount + 3;
        player.Upgrade(clayMineId);
        using (TestCalculator.Defer())
        {
            if (expectThrow)
            {
                Assert.Throws<BusinessException>(() => player.StartManufacture(clayMineId, groupReceiptId));
            }
            else
            {
                Assert.DoesNotThrow(() => player.StartManufacture(clayMineId, groupReceiptId));
            }
        }
    }

    /// <summary>
    /// Долгие рецепты копки выдают больше ресурса за раз, но и тратят монет ровно столько же, сколько добыто.
    /// </summary>
    /// <param name="mineTypeId">Тип карьера.</param>
    /// <param name="receiptId">Долгий рецепт копки.</param>
    /// <param name="outResourceTypeId">Ожидаемый добываемый ресурс.</param>
    /// <param name="amount">Ожидаемое количество ресурса и монетных затрат.</param>
    [TestCase(5, 14, 4, 8)]
    [TestCase(5, 18, 4, 24)]
    [TestCase(3, 15, 2, 8)]
    public void LongDigProducesCorrectAmountTest(int mineTypeId, int receiptId, int outResourceTypeId, int amount)
    {
        var player = TestPlayer.Create();
        var barakTypeId = 2;
        var coinResourceTypeId = 1;
        player.WithDomik(barakTypeId).WithDomik(barakTypeId).WithDomik(mineTypeId);
        var beforeCoin = player.Resource(coinResourceTypeId);
        player.StartManufacture(5, receiptId);
        var after = player.Resources();
        var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
        var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(outValue, Is.EqualTo(amount));
            Assert.That(coinDiff, Is.EqualTo(-amount));
        }
    }

    /// <summary>
    /// Опциональный инструмент не меняет длительность производства, но при использовании расходует один инструмент со склада.
    /// </summary>
    /// <param name="useOptional">Используется ли опциональный инструмент.</param>
    /// <param name="expectedDuration">Ожидаемая длительность производства в секундах.</param>
    /// <param name="expectedToolLeft">Ожидаемый остаток инструментов после запуска.</param>
    [TestCase(false, 28800, 3)]
    [TestCase(true, 28800, 2)]
    public void OptionalToolPreservesDurationAndConsumesToolTest(bool useOptional, int expectedDuration, int expectedToolLeft)
    {
        var player = TestPlayer.Create();
        var toolResourceTypeId = 8;
        var clayDig8hReceiptId = 14;
        player.WithResource(toolResourceTypeId, 3).WithWorkerTraits();

        var start = DateTimeHelper.GetNowDate();
        using (TestCalculator.Defer())
        {
            player.StartManufacture(2, clayDig8hReceiptId, useOptional);
        }

        var manufacture = player.Manufacture(2);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
            Assert.That(player.Resource(toolResourceTypeId), Is.EqualTo(expectedToolLeft));
        }
    }

    /// <summary>
    /// Применение опционального инструмента увеличивает выход ресурса производства.
    /// </summary>
    /// <param name="useOptional">Используется ли опциональный инструмент.</param>
    /// <param name="expectedClay">Ожидаемое количество добытой глины.</param>
    [TestCase(false, 8)]
    [TestCase(true, 11)]
    public void OptionalToolBoostsOutputTest(bool useOptional, int expectedClay)
    {
        var player = TestPlayer.Create();
        var toolResourceTypeId = 8;
        var clayResourceTypeId = 4;
        var clayDig8hReceiptId = 14;
        player.WithResource(toolResourceTypeId, 1).WithWorkerTraits();

        player.StartManufacture(2, clayDig8hReceiptId, useOptional);

        Assert.That(player.Resource(clayResourceTypeId), Is.EqualTo(expectedClay));
    }

    /// <summary>
    /// Стоимость улучшения кузницы в монетах фиксирована по уровням и не должна дрейфовать при правках баланса.
    /// </summary>
    /// <param name="level">Проверяемый уровень постройки.</param>
    /// <param name="expectedCoin">Ожидаемая стоимость улучшения в монетах.</param>
    [TestCase(1, 400)]
    [TestCase(2, 100)]
    [TestCase(3, 300)]
    [TestCase(4, 1500)]
    [TestCase(5, 9000)]
    public void UpgradeCoinCostPerLevelTest(int level, int expectedCoin)
    {
        var coinResourceTypeId = 1;
        var forge = GetDomikTypes().First(x => x.Id == 1);
        var levelData = forge.Levels.First(x => x.Value == level);
        var coin = levelData.Resources.First(x => x.Type.Id == coinResourceTypeId).Value;
        Assert.That(coin, Is.EqualTo(expectedCoin));
    }

    /// <summary>
    /// Число видов ресурсов, требуемых для улучшения кузницы, растёт с уровнем постройки.
    /// </summary>
    /// <param name="level">Проверяемый уровень постройки.</param>
    /// <param name="expectedResourceCount">Ожидаемое число видов ресурсов в стоимости.</param>
    [TestCase(2, 1)]
    [TestCase(3, 3)]
    [TestCase(4, 5)]
    [TestCase(5, 8)]
    public void UpgradeCostMaterialsShapeTest(int level, int expectedResourceCount)
    {
        var forge = GetDomikTypes().First(x => x.Id == 1);
        var levelData = forge.Levels.First(x => x.Value == level);
        Assert.That(levelData.Resources.Length, Is.EqualTo(expectedResourceCount));
    }

    private static IEnumerable<DomikType> GetDomikTypes()
    {
        return App.Act<ResourceManager, DomikType[]>(m => m.GetDomikTypes());
    }

    private static void SetWeather(int weatherTypeId)
    {
        ClearWeatherSchedule();
        var now = DateTimeHelper.GetNowDate();
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.Add(new()
        {
            WeatherTypeId = weatherTypeId,
            StartDate = now,
            EndDate = now.AddSeconds(WeatherManager.WeatherPeriodSeconds),
        });

        scope.Commit();
    }

    private static void ClearWeatherSchedule()
    {
        using var scope = App.Scope();
        scope.Context.WeatherPeriods.RemoveRange(scope.Context.WeatherPeriods);
        scope.Commit();
    }
}
