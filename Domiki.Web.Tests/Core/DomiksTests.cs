using Domiki.Web.Core;
using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;

namespace Domiki.Web.Tests;

public sealed class DomiksTests
{
    [SetUp]
    public void SetUp()
    {
        SetWeather(WeatherIds.Clear);
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
        var player = TestPlayer.Create()
            .WithDomik(DomikIds.Market);

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
            Assert.That(resources.First().Type.Id, Is.EqualTo(ResourceIds.Coin));
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
                catch (Exception)
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
                    player.Upgrade(StartingDomikIds.Barrack);
                }
                catch (Exception)
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
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100);

        player.WithDomiks(DomikIds.Barrack, 4).WithDomik(DomikIds.ClayMine).Upgrade(7);
        var beforeCoin = player.Resource(ResourceIds.Coin);
        player.StartManufacture(7, ReceiptIds.ClayDigTogether);
        var after = player.Resources();
        var clay = after.First(x => x.Type.Id == ResourceIds.Clay).Value;
        var coinDiff = after.First(x => x.Type.Id == ResourceIds.Coin).Value - beforeCoin;
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
        var startingClayMineId = StartingDomikIds.ClayMine;
        player.WithDomik(DomikIds.ClayMine, 0);
        Assert.DoesNotThrow(() => player.StartManufacture(startingClayMineId, ReceiptIds.ClayDig));
        var clay = player.Resource(ResourceIds.Clay);
        Assert.That(clay, Is.EqualTo(1));
    }

    /// <summary>
    /// Добудем одну глину в глиняном карьере.
    /// </summary>
    [Test]
    public void ManufactureTest()
    {
        var player = TestPlayer.Create();
        player.WithDomik(DomikIds.ClayMine).WithDomik(DomikIds.Barrack);
        var beforeCointResourceValue = player.Resource(ResourceIds.Coin);
        player.StartManufacture(3, ReceiptIds.ClayDig);
        var afterResources = player.Resources();
        var afterClayResourceValue = afterResources.First(x => x.Type.Id == ResourceIds.Clay).Value;
        var afterCointResourceValue = afterResources.First(x => x.Type.Id == ResourceIds.Coin).Value;
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
        player.WithDomik(DomikIds.ClayMine);
        var startingClayMineId = StartingDomikIds.ClayMine;
        var boughtClayMineId = 3;
        using (TestCalculator.Defer())
        {
            player.StartManufacture(boughtClayMineId, ReceiptIds.ClayDig);
            Assert.Throws<BusinessException>(() => player.StartManufacture(startingClayMineId, ReceiptIds.ClayDig), "Exception not throw");
        }
    }

    /// <summary>
    /// Если у рецепта нет опционального слота, флаг использования инструмента игнорируется, и инструмент не расходуется.
    /// </summary>
    [Test]
    public void OptionalToolIgnoredWhenReceiptHasNoOptionalTest()
    {
        const int startTool = 3;

        var player = TestPlayer.Create();
        player.WithResource(ResourceIds.Tool, startTool);

        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig, true);

        var resources = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.First(x => x.Type.Id == ResourceIds.Clay).Value, Is.EqualTo(1));
            Assert.That(resources.First(x => x.Type.Id == ResourceIds.Tool).Value, Is.EqualTo(startTool));
        }
    }

    /// <summary>
    /// Нельзя запросить опциональный инструмент в производстве, если инструмента нет на складе – бросает исключение.
    /// </summary>
    [Test]
    public void OptionalToolRequiresToolTest()
    {
        var player = TestPlayer.Create();

        Assert.Throws<BusinessException>(() => player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h, true));
    }

    /// <summary>
    /// Полная гончарная цепочка: добытая глина превращается в посуду, а её продажа на рынке приносит монеты.
    /// </summary>
    [Test]
    public void PotteryDishesAndSellTest()
    {
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 700);

        player.WithBlueprint(BlueprintIds.Pottery);
        player.WithDomik(DomikIds.Barrack).WithDomik(DomikIds.ClayMine).WithDomik(DomikIds.Barrack).WithDomik(DomikIds.Barrack).Buy(DomikIds.Pottery).Buy(DomikIds.Market);
        player.StartManufacture(4, ReceiptIds.ClayDig8h);
        player.StartManufacture(7, ReceiptIds.MakeDishes);
        var afterDishes = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterDishes.First(x => x.Type.Id == ResourceIds.Dishes).Value, Is.EqualTo(1));
            Assert.That(afterDishes.First(x => x.Type.Id == ResourceIds.Clay).Value, Is.EqualTo(6));
        }

        var beforeSellCoin = afterDishes.First(x => x.Type.Id == ResourceIds.Coin).Value;
        player.StartManufacture(8, ReceiptIds.SellDishes);
        var afterSell = player.Resources();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterSell.First(x => x.Type.Id == ResourceIds.Dishes).Value, Is.Zero);
            // цена продажи посуды по рецепту SellDishes
            Assert.That(afterSell.First(x => x.Type.Id == ResourceIds.Coin).Value - beforeSellCoin, Is.EqualTo(45));
        }
    }

    /// <summary>
    /// Нельзя запустить производство на домике, который ещё строится (уровень 0), – прилетает ошибка «Домик ещё строится».
    /// </summary>
    [Test]
    public void StartManufactureOnUnbuiltDomikThrowsTest()
    {
        var player = TestPlayer.Create();
        player.WithDomik(DomikIds.ClayMine, 0);
        var ex = Throws.Business(() => player.StartManufacture(3, ReceiptIds.ClayDig));
        Assert.That(ex.Message, Is.EqualTo("Домик ещё строится"));
    }

    /// <summary>
    /// Запуск производства, не требующего монет по рецепту, не падает даже при нулевом балансе монет.
    /// </summary>
    [Test]
    public void StartManufactureWithZeroCoinsDoesNotThrowTest()
    {
        var player = TestPlayer.Create();
        player.WithDomik(DomikIds.ClayMine).WithDomik(DomikIds.Barrack);

        var coins = player.Resource(ResourceIds.Coin);
        player.WithResource(ResourceIds.Coin, -coins);
        Assert.That(player.Resource(ResourceIds.Coin), Is.Zero);

        Assert.DoesNotThrow(() => player.StartManufacture(3, ReceiptIds.ClayDig));
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
        player.Upgrade(StartingDomikIds.Barrack);

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
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 70);

        player.WithDomik(DomikIds.Barrack).WithDomik(DomikIds.Barrack).Upgrade(3);
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
        player.WithDomik(DomikIds.Barrack).WithDomik(DomikIds.Barrack).WithDomik(mineTypeId);
        var beforeCoin = player.Resource(ResourceIds.Coin);
        player.StartManufacture(5, receiptId);
        var after = player.Resources();
        var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
        var coinDiff = after.First(x => x.Type.Id == ResourceIds.Coin).Value - beforeCoin;
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
        var player = TestPlayer.Create()
            .WithResource(ResourceIds.Coin, 100);

        var additionalBarakCount = barakCount - 1;
        player.WithDomiks(DomikIds.Barrack, additionalBarakCount).WithDomik(DomikIds.ClayMine);
        var clayMineId = additionalBarakCount + 3;
        player.Upgrade(clayMineId);
        using (TestCalculator.Defer())
        {
            if (expectThrow)
            {
                Assert.Throws<BusinessException>(() => player.StartManufacture(clayMineId, ReceiptIds.ClayDigTogether));
            }
            else
            {
                Assert.DoesNotThrow(() => player.StartManufacture(clayMineId, ReceiptIds.ClayDigTogether));
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
        player.WithDomik(DomikIds.Barrack).WithDomik(DomikIds.Barrack).WithDomik(mineTypeId);
        var beforeCoin = player.Resource(ResourceIds.Coin);
        player.StartManufacture(5, receiptId);
        var after = player.Resources();
        var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
        var coinDiff = after.First(x => x.Type.Id == ResourceIds.Coin).Value - beforeCoin;
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
        player.WithResource(ResourceIds.Tool, 3).WithWorkerTraits();

        var start = DateTimeHelper.GetNowDate();
        using (TestCalculator.Defer())
        {
            player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h, useOptional);
        }

        var manufacture = player.Manufacture(StartingDomikIds.ClayMine);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
            Assert.That(player.Resource(ResourceIds.Tool), Is.EqualTo(expectedToolLeft));
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
        player.WithResource(ResourceIds.Tool, 1).WithWorkerTraits();

        player.StartManufacture(StartingDomikIds.ClayMine, ReceiptIds.ClayDig8h, useOptional);

        Assert.That(player.Resource(ResourceIds.Clay), Is.EqualTo(expectedClay));
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
        var forge = GetDomikTypes().First(x => x.Id == DomikIds.Forge);
        var levelData = forge.Levels.First(x => x.Value == level);
        var coin = levelData.Resources.First(x => x.Type.Id == ResourceIds.Coin).Value;
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
        var forge = GetDomikTypes().First(x => x.Id == DomikIds.Forge);
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
