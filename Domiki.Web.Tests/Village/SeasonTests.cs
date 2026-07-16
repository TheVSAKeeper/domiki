using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;
using Domiki.Web.Models;
using System.Text.Json;

namespace Domiki.Web.Tests
{
    [NonParallelizable]
    public class SeasonTests : TestBase
    {
        private const int BridgeTolokaTypeId = 1;
        private const int StoneResourceTypeId = 2;
        private const int GoldResourceTypeId = 5;
        private const int PlankResourceTypeId = 7;
        private const int FountainDecorTypeId = 4;
        private const int BarracksTypeId = 2;
        private const int ScoutHutDomikTypeId = 11;
        private const int GatheringDomikTypeId = 10;
        private const int ShortScoutId = 1;

        [SetUp]
        public void SetUp()
        {
            ResetToloka();
        }

        [TearDown]
        public void TearDown()
        {
            ResetToloka();
        }

        /// <summary>
        /// Номер сезона увеличивается только по истечении полной длительности сезона от эпохи, а границы сезона точно совпадают с этой длительностью.
        /// </summary>
        /// <param name="offsetSeconds">Смещение от эпохи сезонов в секундах.</param>
        /// <param name="expectedNumber">Ожидаемый номер сезона.</param>
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(SeasonManager.SeasonDurationSeconds - 1, 0)]
        [TestCase(SeasonManager.SeasonDurationSeconds, 1)]
        public void GetCurrentSeasonBoundariesTest(int offsetSeconds, int expectedNumber)
        {
            var date = SeasonManager.SeasonEpoch.AddSeconds(offsetSeconds);

            var season = GetSeason(date);

            Assert.That(season.Number, Is.EqualTo(expectedNumber));
            Assert.That(season.StartDate, Is.EqualTo(SeasonManager.SeasonEpoch.AddSeconds(expectedNumber * SeasonManager.SeasonDurationSeconds)));
            Assert.That((season.EndDate - season.StartDate).TotalSeconds, Is.EqualTo(SeasonManager.SeasonDurationSeconds));
        }

        /// <summary>
        /// Выполнение заказа увеличивает сезонный счётчик заказов игрока на величину денежной награды заказа.
        /// </summary>
        [Test]
        public void CompleteOrderGrowsSeasonOrdersCounterTest()
        {
            var playerId = GetPlayerId();
            var order = GetOrders(playerId).First();
            var need = order.Resources.Single();
            GrantResource(playerId, need.Type.Id, need.Value);
            var season = GetSeason(DateTimeHelper.GetNowDate());

            CompleteOrder(playerId, order.Id);

            Assert.That(GetSeasonCounter(season.Number, playerId, SeasonMetric.Orders), Is.EqualTo(order.RewardCoins));
        }

        /// <summary>
        /// Взнос в толоку увеличивает сезонный счётчик толоки игрока на внесённую величину.
        /// </summary>
        [Test]
        public void ContributeGrowsSeasonTolokaCounterTest()
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 100);
            var season = GetSeason(DateTimeHelper.GetNowDate());

            Contribute(playerId, 40);

            Assert.That(GetSeasonCounter(season.Number, playerId, SeasonMetric.Toloka), Is.EqualTo(40));
        }

        /// <summary>
        /// Завершение экспедиции увеличивает сезонный счётчик экспедиций игрока на единицу.
        /// </summary>
        [Test]
        public void FinishExpeditionGrowsSeasonExpeditionsCounterTest()
        {
            var playerId = GetPlayerId();
            BuyBarracks(playerId, 2);
            GrantResource(playerId, GoldResourceTypeId, 1);
            GrantResource(playerId, PlankResourceTypeId, 2);
            StartExpedition(playerId, ShortScoutId);
            var expedition = GetExpeditions(playerId).Active.Single();
            var season = GetSeason(DateTimeHelper.GetNowDate());

            SetExpeditionFinish(expedition.Id, DateTimeHelper.GetNowDate().AddSeconds(-1));
            FinishExpedition(playerId, expedition.Id, DateTimeHelper.GetNowDate());

            Assert.That(GetSeasonCounter(season.Number, playerId, SeasonMetric.Expeditions), Is.EqualTo(1));
        }

        /// <summary>
        /// Сезонные счётчики хранятся отдельно по номеру сезона: значения из прошлого сезона не переносятся и не смешиваются со следующим.
        /// </summary>
        [Test]
        public void SeasonKeyResetsBetweenSeasonsTest()
        {
            var playerId = GetPlayerId();
            var firstSeasonDate = SeasonManager.SeasonEpoch.AddDays(1);
            var secondSeasonDate = SeasonManager.SeasonEpoch.AddSeconds(SeasonManager.SeasonDurationSeconds + 1);

            IncrementCounter(playerId, SeasonMetric.Toloka, 40, firstSeasonDate);
            IncrementCounter(playerId, SeasonMetric.Toloka, 15, secondSeasonDate);

            var firstSeasonId = GetSeason(firstSeasonDate).Number;
            var secondSeasonId = GetSeason(secondSeasonDate).Number;
            Assert.That(secondSeasonId, Is.EqualTo(firstSeasonId + 1));
            Assert.That(GetSeasonCounter(firstSeasonId, playerId, SeasonMetric.Toloka), Is.EqualTo(40));
            Assert.That(GetSeasonCounter(secondSeasonId, playerId, SeasonMetric.Toloka), Is.EqualTo(15));

            var firstSeasonCounters = GetCounters(firstSeasonId);
            Assert.That(firstSeasonCounters[(playerId, SeasonMetric.Toloka)], Is.EqualTo(40));
        }

        /// <summary>
        /// Список мира отдаёт метаданные текущего сезона и посезонные метрики (заказы, толока, экспедиции, уют) по каждой деревне; у NPC все сезонные метрики всегда нулевые.
        /// </summary>
        [Test]
        public void GetWorldReturnsSeasonMetaAndPerVillageMetricsTest()
        {
            var firstPlayerId = CreateNamedPlayer("Сезон Первая", 0, 1);
            var secondPlayerId = CreateNamedPlayer("Сезон Вторая", 1, 2);
            var season = GetSeason(DateTimeHelper.GetNowDate());
            SetSeasonCounter(season.Number, firstPlayerId, SeasonMetric.Orders, 120);
            SetSeasonCounter(season.Number, firstPlayerId, SeasonMetric.Toloka, 30);
            SetSeasonCounter(season.Number, secondPlayerId, SeasonMetric.Expeditions, 4);
            GrantDecor(firstPlayerId, FountainDecorTypeId, 2);

            var world = GetWorld(firstPlayerId);

            Assert.That(world.Season.Number, Is.EqualTo(season.Number));
            Assert.That(world.Season.StartDate, Is.EqualTo(season.StartDate));
            Assert.That(world.Season.EndDate, Is.EqualTo(season.EndDate));

            var firstVillage = world.Villages.Single(x => x.PlayerId == firstPlayerId);
            Assert.That(firstVillage.SeasonOrders, Is.EqualTo(120));
            Assert.That(firstVillage.SeasonToloka, Is.EqualTo(30));
            Assert.That(firstVillage.SeasonExpeditions, Is.EqualTo(0));
            Assert.That(firstVillage.Comfort, Is.GreaterThan(0));

            var secondVillage = world.Villages.Single(x => x.PlayerId == secondPlayerId);
            Assert.That(secondVillage.SeasonExpeditions, Is.EqualTo(4));
            Assert.That(secondVillage.Comfort, Is.EqualTo(0));

            var npcs = world.Villages.Where(x => x.IsNpc).ToArray();
            Assert.That(npcs.All(x => x.SeasonOrders == 0 && x.SeasonToloka == 0 && x.SeasonExpeditions == 0 && x.Comfort == 0), Is.True);
        }

        /// <summary>
        /// DTO мира с сезонными полями не утекает приватные поля игрока (Name, AspNetUserId) в сериализованный JSON.
        /// </summary>
        [Test]
        public void WorldDtoWithSeasonFieldsDoesNotContainPrivateFieldsTest()
        {
            var playerId = CreateNamedPlayer("Сезон Приват", 5, 6);
            SetSeasonCounter(GetSeason(DateTimeHelper.GetNowDate()).Number, playerId, SeasonMetric.Orders, 10);

            WorldDto worldDto;
            using (var uow = GetUow())
            {
                var manager = GetWorldManager(uow);
                worldDto = manager.GetWorld(playerId).ToDto();
                uow.Commit();
            }

            var json = JsonSerializer.Serialize(worldDto);
            Assert.That(json, Does.Not.Contain("\"Name\""));
            Assert.That(json, Does.Not.Contain("AspNetUserId"));
        }

        /// <summary>
        /// Параллельные взносы в толоку от одного игрока суммируются в сезонном счётчике точно, без потери обновлений.
        /// </summary>
        [Test]
        public async Task ConcurrentContributesKeepExactSeasonCounterSumTest()
        {
            var playerId = GetUnlockedPlayerId();
            GrantResource(playerId, StoneResourceTypeId, 1000);
            var season = GetSeason(DateTimeHelper.GetNowDate());

            await Task.WhenAll(
                Task.Run(() => Contribute(playerId, 70)),
                Task.Run(() => Contribute(playerId, 80)));

            Assert.That(GetSeasonCounter(season.Number, playerId, SeasonMetric.Toloka), Is.EqualTo(150));
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var playerId = domikManager.GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private int CreateNamedPlayer(string prefix, int crestIcon, int crestColor)
        {
            var playerId = GetPlayerId();
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.SetVillageIdentity(playerId, TestVillageName(prefix), crestIcon, crestColor);
                uow.Commit();
            }

            return playerId;
        }

        private World GetWorld(int currentPlayerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetWorldManager(uow);
                var world = manager.GetWorld(currentPlayerId);
                uow.Commit();
                return world;
            }
        }

        private Order[] GetOrders(int playerId)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                var orders = orderManager.GetOrders(playerId).ToArray();
                uow.Commit();
                return orders;
            }
        }

        private void CompleteOrder(int playerId, int orderId)
        {
            using (var uow = GetUow())
            {
                var orderManager = GetOrderManager(uow);
                orderManager.CompleteOrder(playerId, orderId);
                uow.Commit();
            }
        }

        private int GetUnlockedPlayerId()
        {
            var playerId = GetPlayerId();
            GrantDecor(playerId, FountainDecorTypeId, 4);
            GrantResource(playerId, 1, 800);
            BuyDomik(playerId, GatheringDomikTypeId);
            return playerId;
        }

        private void Contribute(int playerId, int amount)
        {
            using (var uow = GetUow())
            {
                var manager = GetTolokaManager(uow);
                manager.Contribute(playerId, amount, DateTimeHelper.GetNowDate());
                uow.Commit();
            }
        }

        private void GrantDecor(int playerId, int decorTypeId, int count)
        {
            using (var uow = GetUow())
            {
                var decor = uow.Context.PlayerDecors.SingleOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);
                if (decor == null)
                {
                    decor = new Domiki.Web.Data.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
                    uow.Context.PlayerDecors.Add(decor);
                }

                decor.Count += count;
                uow.Context.SaveChanges();
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

        private void BuyBarracks(int playerId, int count)
        {
            using (var uow = GetUow())
            {
                var nextId = (uow.Context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0) + 1;
                for (var i = 0; i < count; i++)
                {
                    uow.Context.Domiks.Add(new Domiki.Web.Data.Domik { PlayerId = playerId, Id = nextId + i, TypeId = BarracksTypeId, Level = 1 });
                }
                uow.Commit();
            }
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                domikManager.BuyDomik(playerId, typeId);
                uow.Commit();
            }
        }

        private void AddBuiltDomik(int playerId, int typeId)
        {
            using (var uow = GetUow())
            {
                if (!uow.Context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == typeId))
                {
                    uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                    {
                        PlayerId = playerId,
                        Id = -typeId,
                        TypeId = typeId,
                        Level = 1,
                    });
                    uow.Context.SaveChanges();
                }

                uow.Commit();
            }
        }

        private ExpeditionState? GetExpeditions(int playerId)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow);
                var state = manager.GetExpeditions(playerId);
                uow.Commit();
                return state;
            }
        }

        private void StartExpedition(int playerId, int expeditionTypeId)
        {
            AddBuiltDomik(playerId, ScoutHutDomikTypeId);
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow, calculatorJustFinishMode: false);
                manager.StartExpedition(playerId, expeditionTypeId);
                uow.Commit();
            }
        }

        private void FinishExpedition(int playerId, int expeditionId, DateTime date)
        {
            using (var uow = GetUow())
            {
                var manager = GetExpeditionManager(uow, calculatorJustFinishMode: false);
                var result = manager.FinishExpedition(date, new CalculateInfo
                {
                    PlayerId = playerId,
                    ObjectId = expeditionId,
                    Date = date,
                    Type = CalculateTypes.Expedition,
                });
                Assert.That(result, Is.True);
                uow.Commit();
            }
        }

        private void SetExpeditionFinish(int expeditionId, DateTime finishDate)
        {
            using (var uow = GetUow())
            {
                var expedition = uow.Context.Expeditions.Single(x => x.Id == expeditionId);
                expedition.FinishDate = finishDate;
                uow.Commit();
            }
        }

        private void SetSeasonCounter(int seasonId, int playerId, SeasonMetric metric, int value)
        {
            using (var uow = GetUow())
            {
                var counter = uow.Context.SeasonCounters.SingleOrDefault(x => x.SeasonId == seasonId && x.PlayerId == playerId && x.Metric == (int)metric);
                if (counter == null)
                {
                    counter = new Domiki.Web.Data.SeasonCounter { SeasonId = seasonId, PlayerId = playerId, Metric = (int)metric };
                    uow.Context.SeasonCounters.Add(counter);
                }

                counter.Value = value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private int GetSeasonCounter(int seasonId, int playerId, SeasonMetric metric)
        {
            using (var uow = GetUow())
            {
                return uow.Context.SeasonCounters
                    .Where(x => x.SeasonId == seasonId && x.PlayerId == playerId && x.Metric == (int)metric)
                    .Select(x => x.Value)
                    .SingleOrDefault();
            }
        }

        private void IncrementCounter(int playerId, SeasonMetric metric, int value, DateTime date)
        {
            using (var uow = GetUow())
            {
                var manager = GetSeasonManager(uow);
                manager.IncrementCounter(playerId, metric, value, date);
                uow.Commit();
            }
        }

        private Season GetSeason(DateTime date)
        {
            using (var uow = GetUow())
            {
                return GetSeasonManager(uow).GetCurrentSeason(date);
            }
        }

        private Dictionary<(int PlayerId, SeasonMetric Metric), int> GetCounters(int seasonId)
        {
            using (var uow = GetUow())
            {
                return GetSeasonManager(uow).GetCounters(seasonId);
            }
        }

        private void ResetToloka()
        {
            using (var uow = GetUow())
            {
                uow.Context.TolokaContributions.RemoveRange(uow.Context.TolokaContributions);
                uow.Context.Tolokas.RemoveRange(uow.Context.Tolokas);
                uow.Context.SaveChanges();
                uow.Context.Tolokas.Add(new Domiki.Web.Data.Toloka
                {
                    TolokaTypeId = BridgeTolokaTypeId,
                    Collected = 0,
                    Goal = 2000,
                    StartDate = DateTimeHelper.GetNowDate(),
                    CompletedDate = null,
                });
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }
    }
}
