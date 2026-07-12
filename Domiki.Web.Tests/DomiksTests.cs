using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class DomiksTests : TestBase
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
        [Test]
        public void GetPlayerIdTest()
        {
            var playerId = GetPlayerId();
            Assert.Greater(playerId, 0);
        }

        [Test]
        public void CheckBaseResourcesTest()
        {
            var playerId = GetPlayerId();
            var resources = GetResources(playerId);
            Assert.That(resources.Count, Is.EqualTo(1));
            Assert.That(resources.First().Type.Id, Is.EqualTo(1));
            Assert.That(resources.First().Value, Is.EqualTo(DomikManager.StartingCoins));
        }

        /// <summary>
        /// �������� ����� � ���������, ��� � ��� 1 ����� ������� ������.
        /// </summary>
        [Test]
        public void BuyDomikTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 3, MarketTypeId);
            var beforeResources = GetResources(playerId);
            var types = GetDomikTypes();
            var buyType = types.First(x => x.UnlockLevel == 0);
            BuyDomik(playerId, buyType.Id);

            var afterResources = GetResources(playerId);
            var domiks = GetDomiks(playerId);
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
        /// �������� �� ��, ��� ������������� ������� �� ����� ��������� �����
        /// </summary>
        [Test]
        public void ConcurrencyBuyDomikTest()
        {
            for (var i = 1; i <= 217; i++)
            {
                var playerId = GetPlayerId();
                var types = GetDomikTypes();
                var domikType = types.First(x => x.UnlockLevel == 0 && x.MaxCount == 1);
                Assert.That(domikType.MaxCount, Is.EqualTo(1));
                var domikTypeId = domikType.Id;

                var numbers = Enumerable.Range(0, 10).ToList();
                Parallel.ForEach(numbers, number =>
                {
                    try
                    {
                        BuyDomik(playerId, domikTypeId);
                    }
                    catch (Exception ex)
                    {
                    }
                });

                var domiks = GetDomiks(playerId);
                var domiksCount = domiks.Count();
                Assert.That(domiksCount, Is.EqualTo(3), "iterarion number " + i);
            }
        }

        [Test]
        public void UpgradeDomikTest()
        {
            var playerId = GetPlayerId();
            var types = GetDomikTypes();
            var buyType = types.First(x => x.UnlockLevel == 0);
            var beforeResources = GetResources(playerId);
            UpgradeDomik(playerId, 1);

            var domiks = GetDomiks(playerId);
            var level = domiks.First().Level;
            Assert.That(level, Is.EqualTo(2));

            var afterResources = GetResources(playerId);

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
        /// �������� �� ��, ��� ������������� ������� ��������� �������� �����
        /// </summary>
        [Test]
        public void ConcurrencyUpgradeDomikTest()
        {
            for (var i = 1; i <= 217; i++)
            {
                var playerId = GetPlayerId();
                var types = GetDomikTypes();
                var domikType = types.First(x => x.UnlockLevel == 0 && x.MaxCount == 1);
                Assert.That(domikType.MaxCount, Is.EqualTo(1));
                var domikTypeId = domikType.Id;
                BuyDomik(playerId, domikTypeId);

                var actionCount = 4;
                var numbers = Enumerable.Range(0, actionCount).ToList();
                var errorCount = 0;
                Parallel.ForEach(numbers, number =>
                {
                    try
                    {
                        UpgradeDomik(playerId, 1);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                    }
                });

                var domiks = GetDomiks(playerId);
                var level = domiks.First().Level;
                var checkValue = level + errorCount;
                var expected = 1 + actionCount;

                // ������� ���� ����� ��������
                Assert.That(checkValue, Is.GreaterThan(1));

                // ���������� �������� �������� + 1 ������� ������� + ���������� ������ ����� = 1 ������� ������� + ���������� �������
                Assert.That(checkValue, Is.EqualTo(expected), "iterarion number " + i + ", checkValue " + checkValue + ",  error count " + errorCount);
            }
        }

        /// <summary>
        /// ������� ���� ����� � �������� �������.
        /// </summary>
        [Test]
        public void ManufactureTest()
        {
            var playerId = GetPlayerId();
            var clayMineId = 5;
            var barakTypeId = 2;
            GrantDomik(playerId, 3, clayMineId);
            GrantDomik(playerId, 4, barakTypeId);
            var coinResourceTypeId = 1;
            var clayResourceTypeId = 4;
            var clayDigReceiptId = 1;
            var beforeResources = GetResources(playerId);
            var beforeCointResourceValue = beforeResources.First(x => x.Type.Id == coinResourceTypeId).Value;
            StartManufacture(playerId, 3, clayDigReceiptId);
            var afterResources = GetResources(playerId);
            var afterClayResourceValue = afterResources.First(x => x.Type.Id == clayResourceTypeId).Value;
            var afterCointResourceValue = afterResources.First(x => x.Type.Id == coinResourceTypeId).Value;
            var coinDiff = afterCointResourceValue - beforeCointResourceValue;
            Assert.That(afterClayResourceValue, Is.EqualTo(1));
            Assert.That(coinDiff, Is.EqualTo(0));
        }

        [Test]
        public void StartManufactureWithZeroCoinsDoesNotThrowTest()
        {
            var playerId = GetPlayerId();
            var clayMineTypeId = 5;
            var barakTypeId = 2;
            var coinResourceTypeId = 1;
            var clayDigReceiptId = 1;
            GrantDomik(playerId, 3, clayMineTypeId);
            GrantDomik(playerId, 4, barakTypeId);

            var coins = GetResources(playerId).First(x => x.Type.Id == coinResourceTypeId).Value;
            GrantResource(playerId, coinResourceTypeId, -coins);
            Assert.That(GetResources(playerId).First(x => x.Type.Id == coinResourceTypeId).Value, Is.EqualTo(0));

            Assert.DoesNotThrow(() => StartManufacture(playerId, 3, clayDigReceiptId));
        }

        /// <summary>
        /// ���� ����� ��� ����� ������ ��������, ������ ��������� ��� ������������ � ����� �������.
        /// </summary>
        [Test]
        public void MaxWorkerCountTest()
        {
            var playerId = GetPlayerId();
            var clayMineTypeId = 5;
            GrantDomik(playerId, 3, clayMineTypeId);
            var clayDigReceiptId = 1;
            var startingClayMineId = 2;
            var boughtClayMineId = 3;
            StartManufacture(playerId, boughtClayMineId, clayDigReceiptId, false);
            Assert.Throws<BusinessException>(() => StartManufacture(playerId, startingClayMineId, clayDigReceiptId, false), "Exception not throw");
        }

        [TestCase(3, 4, 2)]
        [TestCase(6, 5, 3)]
        public void DigProducesCorrectResourceTest(int mineTypeId, int receiptId, int outResourceTypeId)
        {
            var playerId = GetPlayerId();
            var barakTypeId = 2;
            var coinResourceTypeId = 1;
            GrantDomik(playerId, 3, barakTypeId);
            GrantDomik(playerId, 4, barakTypeId);
            GrantDomik(playerId, 5, mineTypeId);
            var beforeCoin = GetResources(playerId).First(x => x.Type.Id == coinResourceTypeId).Value;
            StartManufacture(playerId, 5, receiptId);
            var after = GetResources(playerId);
            var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
            var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
            Assert.That(outValue, Is.EqualTo(1));
            Assert.That(coinDiff, Is.EqualTo(0));
        }

        [TestCase(5, false)]
        [TestCase(4, true)]
        public void GroupRecipePlodderCountHonoredTest(int barakCount, bool expectThrow)
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 100);
            var barakTypeId = 2;
            var clayMineTypeId = 5;
            var groupReceiptId = 2;
            var additionalBarakCount = barakCount - 1;
            for (var i = 0; i < additionalBarakCount; i++)
            {
                GrantDomik(playerId, 3 + i, barakTypeId);
            }
            GrantDomik(playerId, 3 + additionalBarakCount, clayMineTypeId);
            var clayMineId = additionalBarakCount + 3;
            UpgradeDomik(playerId, clayMineId);
            if (expectThrow)
            {
                Assert.Throws<BusinessException>(() => StartManufacture(playerId, clayMineId, groupReceiptId, false));
            }
            else
            {
                Assert.DoesNotThrow(() => StartManufacture(playerId, clayMineId, groupReceiptId, false));
            }
        }

        [Test]
        public void GroupRecipeOutputPremiumTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 100);
            var barakTypeId = 2;
            var clayMineTypeId = 5;
            var groupReceiptId = 2;
            var coinResourceTypeId = 1;
            var clayResourceTypeId = 4;
            for (var i = 0; i < 4; i++)
            {
                GrantDomik(playerId, 3 + i, barakTypeId);
            }
            GrantDomik(playerId, 7, clayMineTypeId);
            UpgradeDomik(playerId, 7);
            var beforeCoin = GetResources(playerId).First(x => x.Type.Id == coinResourceTypeId).Value;
            StartManufacture(playerId, 7, groupReceiptId);
            var after = GetResources(playerId);
            var clay = after.First(x => x.Type.Id == clayResourceTypeId).Value;
            var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
            Assert.That(clay, Is.EqualTo(8));
            Assert.That(coinDiff, Is.EqualTo(-5));
        }

        [Test]
        public void LevelZeroDomikDoesNotBreakManufactureTest()
        {
            var playerId = GetPlayerId();
            var clayMineTypeId = 5;
            var clayDigReceiptId = 1;
            var clayResourceTypeId = 4;
            var startingClayMineId = 2;
            GrantDomik(playerId, 3, clayMineTypeId, level: 0);
            Assert.DoesNotThrow(() => StartManufacture(playerId, startingClayMineId, clayDigReceiptId));
            var clay = GetResources(playerId).First(x => x.Type.Id == clayResourceTypeId).Value;
            Assert.That(clay, Is.EqualTo(1));
        }

        [Test]
        public void StartManufactureOnUnbuiltDomikThrowsTest()
        {
            var playerId = GetPlayerId();
            var clayMineTypeId = 5;
            var clayDigReceiptId = 1;
            GrantDomik(playerId, 3, clayMineTypeId, level: 0);
            var ex = Assert.Throws<BusinessException>(() => StartManufacture(playerId, 3, clayDigReceiptId));
            Assert.That(ex.Message, Is.EqualTo("Домик ещё строится"));
        }

        [TestCase(5, 14, 4, 8)]
        [TestCase(5, 18, 4, 24)]
        [TestCase(3, 15, 2, 8)]
        public void LongDigProducesCorrectAmountTest(int mineTypeId, int receiptId, int outResourceTypeId, int amount)
        {
            var playerId = GetPlayerId();
            var barakTypeId = 2;
            var coinResourceTypeId = 1;
            GrantDomik(playerId, 3, barakTypeId);
            GrantDomik(playerId, 4, barakTypeId);
            GrantDomik(playerId, 5, mineTypeId);
            var beforeCoin = GetResources(playerId).First(x => x.Type.Id == coinResourceTypeId).Value;
            StartManufacture(playerId, 5, receiptId);
            var after = GetResources(playerId);
            var outValue = after.First(x => x.Type.Id == outResourceTypeId).Value;
            var coinDiff = after.First(x => x.Type.Id == coinResourceTypeId).Value - beforeCoin;
            Assert.That(outValue, Is.EqualTo(amount));
            Assert.That(coinDiff, Is.EqualTo(-amount));
        }

        [Test]
        public void UpgradeToLevel3RequiresMaterialsTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 70);
            GrantDomik(playerId, 3, 2);
            GrantDomik(playerId, 4, 2);
            UpgradeDomik(playerId, 3);
            Assert.Throws<BusinessException>(() => UpgradeDomik(playerId, 3));
        }

        [Test]
        public void PotteryDishesAndSellTest()
        {
            var playerId = GetPlayerId();
            GrantResource(playerId, 1, 700);
            var potteryBlueprintId = 3;
            GrantBlueprint(playerId, potteryBlueprintId);
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
            GrantDomik(playerId, 3, barakTypeId);
            GrantDomik(playerId, 4, clayMineTypeId);
            GrantDomik(playerId, 5, barakTypeId);
            GrantDomik(playerId, 6, barakTypeId);
            BuyDomik(playerId, potteryTypeId);
            BuyDomik(playerId, marketTypeId);
            StartManufacture(playerId, 4, clayDig8hReceiptId);
            StartManufacture(playerId, 7, makeDishesReceiptId);
            var afterDishes = GetResources(playerId);
            Assert.That(afterDishes.First(x => x.Type.Id == dishesResourceTypeId).Value, Is.EqualTo(1));
            Assert.That(afterDishes.First(x => x.Type.Id == clayResourceTypeId).Value, Is.EqualTo(6));
            var beforeSellCoin = afterDishes.First(x => x.Type.Id == coinResourceTypeId).Value;
            StartManufacture(playerId, 8, sellDishesReceiptId);
            var afterSell = GetResources(playerId);
            Assert.That(afterSell.First(x => x.Type.Id == dishesResourceTypeId).Value, Is.EqualTo(0));
            Assert.That(afterSell.First(x => x.Type.Id == coinResourceTypeId).Value - beforeSellCoin, Is.EqualTo(45));
        }

        [TestCase(false, 28800, 3)]
        [TestCase(true, 28800, 2)]
        public void OptionalToolPreservesDurationAndConsumesToolTest(bool useOptional, int expectedDuration, int expectedToolLeft)
        {
            var playerId = GetPlayerId();
            var toolResourceTypeId = 8;
            var clayDig8hReceiptId = 14;
            GrantResource(playerId, toolResourceTypeId, 3);
            ResetWorkerTraits(playerId);

            var start = DateTimeHelper.GetNowDate();
            StartManufacture(playerId, 2, clayDig8hReceiptId, false, useOptional);

            var manufacture = GetDomiks(playerId).First(x => x.Id == 2).Manufactures.Single();
            Assert.That((manufacture.FinishDate - start).TotalSeconds, Is.EqualTo(expectedDuration).Within(2));
            Assert.That(GetResources(playerId).First(x => x.Type.Id == toolResourceTypeId).Value, Is.EqualTo(expectedToolLeft));
        }

        [TestCase(false, 8)]
        [TestCase(true, 11)]
        public void OptionalToolBoostsOutputTest(bool useOptional, int expectedClay)
        {
            var playerId = GetPlayerId();
            var toolResourceTypeId = 8;
            var clayResourceTypeId = 4;
            var clayDig8hReceiptId = 14;
            GrantResource(playerId, toolResourceTypeId, 1);
            ResetWorkerTraits(playerId);

            StartManufacture(playerId, 2, clayDig8hReceiptId, true, useOptional);

            var clay = GetResources(playerId).Single(x => x.Type.Id == clayResourceTypeId);
            Assert.That(clay.Value, Is.EqualTo(expectedClay));
        }

        [Test]
        public void OptionalToolRequiresToolTest()
        {
            var playerId = GetPlayerId();
            var clayDig8hReceiptId = 14;

            Assert.Throws<BusinessException>(() => StartManufacture(playerId, 2, clayDig8hReceiptId, useOptional: true));
        }

        [Test]
        public void OptionalToolIgnoredWhenReceiptHasNoOptionalTest()
        {
            var playerId = GetPlayerId();
            var clayResourceTypeId = 4;
            var toolResourceTypeId = 8;
            var clayDigReceiptId = 1;
            GrantResource(playerId, toolResourceTypeId, 3);

            StartManufacture(playerId, 2, clayDigReceiptId, useOptional: true);

            var resources = GetResources(playerId);
            Assert.That(resources.First(x => x.Type.Id == clayResourceTypeId).Value, Is.EqualTo(1));
            Assert.That(resources.First(x => x.Type.Id == toolResourceTypeId).Value, Is.EqualTo(3));
        }

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

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                MuteFtue(playerId);
                return playerId;
            }
        }

        private IEnumerable<Resource> GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var resources = domikManager.GetResources(playerId);
                uow.Commit();
                return resources;
            }
        }

        private IEnumerable<Domik> GetDomiks(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var domiks = domikManager.GetDomiks(playerId);
                return domiks;
            }
        }
        private IEnumerable<DomikType> GetDomikTypes()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetResourceManager(uow);
                var types = domikManager.GetDomikTypes();
                return types;
            }
        }

        private void UpgradeDomik(int playerId, int domikId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.UpgradeDomik(playerId, domikId);
                uow.Commit();
            }
        }

        private void BuyDomik(int playerId, int domikTypeId, bool calculatorJustFinishMod = true)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMod);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = typeId };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void GrantBlueprint(int playerId, int blueprintId)
        {
            using (var uow = GetUow())
            {
                uow.Context.PlayerBlueprints.Add(new Domiki.Web.Data.PlayerBlueprint { PlayerId = playerId, BlueprintId = blueprintId });
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void ResetWorkerTraits(int playerId)
        {
            using (var uow = GetUow())
            {
                GetWorkerManager(uow).EnsureWorkers(playerId);
                foreach (var worker in uow.Context.Workers.Where(x => x.PlayerId == playerId).ToArray())
                {
                    worker.TraitId = 1;
                }
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void SetWeather(int weatherTypeId)
        {
            ClearWeatherSchedule();
            var now = DateTimeHelper.GetNowDate();
            using (var uow = GetUow())
            {
                uow.Context.WeatherPeriods.Add(new Domiki.Web.Data.WeatherPeriod
                {
                    WeatherTypeId = weatherTypeId,
                    StartDate = now,
                    EndDate = now.AddSeconds(WeatherManager.WeatherPeriodSeconds),
                });
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void ClearWeatherSchedule()
        {
            using (var uow = GetUow())
            {
                uow.Context.WeatherPeriods.RemoveRange(uow.Context.WeatherPeriods);
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId, bool calculatorJustFinishMod = true, bool useOptional = false)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMod);
                domikManager.StartManufacture(playerId, domikId, receiptId, useOptional);
                uow.Commit();
            }
        }
    }
}
