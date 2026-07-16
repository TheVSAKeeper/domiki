using System.Text.Json;
using Domiki.Web.Business.Core;
using Domiki.Web.Data;

namespace Domiki.Web.Tests
{
    public class GoalsTests : TestBase
    {
        private const int BarracksDomikId = 1;
        private const int ClayMineDomikId = 2;
        private const int BarracksTypeId = 2;
        private const int StoneMineTypeId = 3;
        private const int LumberMillTypeId = 6;
        private const int MarketTypeId = 7;
        private const int ClayDigReceiptId = 1;
        private const int ClayDigEightHoursReceiptId = 14;
        private const int SellClayReceiptId = 6;
        private const int ClayResourceTypeId = 4;
        private const int CoinResourceTypeId = 1;

        /// <summary>
        /// Из 9 целей первая закрывается добычей глины, платит награду монетами и пишет запись в журнал с id цели и наградой.
        /// </summary>
        [Test]
        public void FirstGoalCompletesOnClayDigAndWritesJournalEntryTest()
        {
            var playerId = CreatePlayer();

            var initial = GetGoalsState(playerId);
            StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId);
            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(initial.ActiveGoal!.Ordinal, Is.EqualTo(1));
                Assert.That(initial.CompletedCount, Is.Zero);
                Assert.That(initial.TotalCount, Is.EqualTo(9));
                Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1 }));
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(2));
                Assert.That(state.CompletedCount, Is.EqualTo(1));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId), Is.EqualTo(210));
            });

            var entry = GetGoalEvent(playerId, 1);
            using var data = JsonDocument.Parse(entry.Data);
            Assert.That(data.RootElement.GetProperty("goalId").GetInt32(), Is.EqualTo(1));
            Assert.That(data.RootElement.GetProperty("rewardCoins").GetInt32(), Is.EqualTo(10));
        }

        /// <summary>
        /// Покупка рынка выполняет условие второй цели, но начисление награды и её видимость в списке завершённых происходит только при чтении состояния целей.
        /// </summary>
        [Test]
        public void MarketPurchaseCompletesSecondGoalOnlyWhenGoalsAreReadTest()
        {
            var playerId = CreatePlayer();
            CompleteClayDigGoal(playerId);

            BuyDomik(playerId, MarketTypeId);
            Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1 }));

            var coinsBeforeGoalsRead = GetResourceValue(playerId, CoinResourceTypeId);
            var state = GetGoalsState(playerId);
            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(3));
                Assert.That(state.CompletedCount, Is.EqualTo(2));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId) - coinsBeforeGoalsRead, Is.EqualTo(20));
            });
        }

        /// <summary>
        /// Продажа глины, случившаяся до активации третьей цели, в зачёт не идёт: цель закрывает только следующая продажа после активации.
        /// </summary>
        [Test]
        public void SaleBeforeItsActivationNeedsSecondSaleToCompleteThirdGoalTest()
        {
            var playerId = CreatePlayer();
            GrantAllResources(playerId, 1000);
            BuyDomik(playerId, MarketTypeId);
            GrantResource(playerId, ClayResourceTypeId, 10);
            var marketId = GetDomikId(playerId, MarketTypeId);

            StartManufacture(playerId, marketId, SellClayReceiptId);
            var afterFirstSale = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1, 2 }));
                Assert.That(afterFirstSale.ActiveGoal!.Ordinal, Is.EqualTo(3));
            });

            var coinsBeforeSecondSale = GetResourceValue(playerId, CoinResourceTypeId);
            StartManufacture(playerId, marketId, SellClayReceiptId, false);
            var afterSecondSale = GetGoalsState(playerId);

            Assert.That(afterSecondSale.ActiveGoal!.Ordinal, Is.EqualTo(4));
            Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(GetResourceValue(playerId, CoinResourceTypeId) - coinsBeforeSecondSale, Is.EqualTo(15));
        }

        /// <summary>
        /// Одна продажа глины может одновременно закрыть цель-состояние «есть рынок» и цель-действие «продать глину», если предыдущие цели уже выполнены.
        /// </summary>
        [Test]
        public void SaleCompletesMarketStateGoalAndSaleGoalTogetherTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1);
            GrantAllResources(playerId, 1000);
            BuyDomik(playerId, MarketTypeId);
            GrantResource(playerId, ClayResourceTypeId, 10);

            StartManufacture(playerId, GetDomikId(playerId, MarketTypeId), SellClayReceiptId);
            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(4));
            });
        }

        /// <summary>
        /// Прокачка казармы закрывает пятую цель, но только после того, как цели с первой по четвёртую уже выполнены.
        /// </summary>
        [Test]
        public void BarracksUpgradeCompletesFifthGoalAfterEarlierGoalsTest()
        {
            var playerId = CreatePlayer();
            GrantAllResources(playerId, 1000);
            CompleteClayDigGoal(playerId);
            BuyDomik(playerId, MarketTypeId);
            GetGoalsState(playerId);
            GrantResource(playerId, ClayResourceTypeId, 10);
            StartManufacture(playerId, GetDomikId(playerId, MarketTypeId), SellClayReceiptId);
            BuyDomik(playerId, LumberMillTypeId);
            GetGoalsState(playerId);

            UpgradeDomik(playerId, BarracksDomikId);
            var coinsBeforeGoalsRead = GetResourceValue(playerId, CoinResourceTypeId);
            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(GetCompletedGoalIds(playerId), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(6));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId) - coinsBeforeGoalsRead, Is.EqualTo(50));
            });
        }

        /// <summary>
        /// Седьмую цель закрывает только восьмичасовая смена добычи глины, обычная часовая смена её не выполняет.
        /// </summary>
        [Test]
        public void EightHourShiftCompletesSeventhGoalButOneHourShiftDoesNotTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1, 2, 3, 4, 5, 6);

            StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId);
            Assert.That(GetGoalsState(playerId).ActiveGoal!.Ordinal, Is.EqualTo(7));

            StartManufacture(playerId, ClayMineDomikId, ClayDigEightHoursReceiptId);
            Assert.That(GetGoalsState(playerId).ActiveGoal!.Ordinal, Is.EqualTo(8));
            Assert.That(GetCompletedGoalIds(playerId), Does.Contain(7));
        }

        /// <summary>
        /// Цели-состояния закрываются каскадом автоматически, но каскад останавливается на первой цели-действии, требующей отдельного игрового события.
        /// </summary>
        [Test]
        public void StateGoalsCascadeOnlyUntilActionGoalTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1);
            GrantBuiltDomik(playerId, MarketTypeId, 1);
            GrantBuiltDomik(playerId, LumberMillTypeId, 1);
            SetDomikLevel(playerId, BarracksDomikId, 2);

            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(3));
                Assert.That(state.CompletedCount, Is.EqualTo(2));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId), Is.EqualTo(220));
            });
        }

        /// <summary>
        /// Выполнение заказа закрывает шестую цель.
        /// </summary>
        [Test]
        public void CompletedOrderCompletesSixthGoalTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1, 2, 3, 4, 5);
            var order = GetOrders(playerId).First(x => x.Resources.Single().Type.Id == ClayResourceTypeId);
            var required = order.Resources.Single();
            GrantResource(playerId, required.Type.Id, required.Value);

            CompleteOrder(playerId, order.Id);
            var state = GetGoalsState(playerId);

            Assert.That(GetCompletedGoalIds(playerId), Does.Contain(6));
            Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(7));
        }

        /// <summary>
        /// Покупка каменоломни закрывает восьмую цель, а награда материализуется при следующем чтении состояния целей.
        /// </summary>
        [Test]
        public void StoneMinePurchaseCompletesEighthGoalWhenGoalsAreReadTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1, 2, 3, 4, 5, 6, 7);
            GrantAllResources(playerId, 1000);
            GrantBuiltDomik(playerId, MarketTypeId, 1);
            GrantBuiltDomik(playerId, LumberMillTypeId, 1);

            BuyDomik(playerId, StoneMineTypeId);
            var coinsBeforeGoalsRead = GetResourceValue(playerId, CoinResourceTypeId);
            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveGoal!.Ordinal, Is.EqualTo(9));
                Assert.That(GetCompletedGoalIds(playerId), Does.Contain(8));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId) - coinsBeforeGoalsRead, Is.EqualTo(40));
            });
        }

        /// <summary>
        /// Достижение 10 уровня деревни закрывает девятую, последнюю цель – активных целей после этого не остаётся.
        /// </summary>
        [Test]
        public void VillageLevelTenCompletesNinthGoalTest()
        {
            var playerId = CreatePlayer();
            SeedCompletedGoals(playerId, 1, 2, 3, 4, 5, 6, 7, 8);
            GrantReputation(playerId, 1, 20);

            Assert.That(GetVillageLevel(playerId), Is.GreaterThanOrEqualTo(10));
            var state = GetGoalsState(playerId);

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveGoal, Is.Null);
                Assert.That(state.CompletedCount, Is.EqualTo(9));
                Assert.That(GetResourceValue(playerId, CoinResourceTypeId), Is.EqualTo(250));
            });
        }

        private int CreatePlayer()
        {
            using var uow = GetUow();
            var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private Domiki.Web.Business.Models.GoalsState GetGoalsState(int playerId)
        {
            using var uow = GetUow();
            var resourceManager = GetResourceManager(uow);
            var playerResourceManager = new PlayerResourceManager(uow.Context, resourceManager);
            var goalManager = new GoalManager(uow.Context, resourceManager, playerResourceManager, GetVillageLevelCalculator(uow), GetPlayerEventManager(uow));
            var state = goalManager.GetGoalsState(playerId);
            uow.Commit();
            return state;
        }

        private void CompleteClayDigGoal(int playerId) => StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId);

        private void StartManufacture(int playerId, int domikId, int receiptId, bool justFinish = true)
        {
            using var uow = GetUow();
            GetDomikManager(uow, justFinish).StartManufacture(playerId, domikId, receiptId);
            uow.Commit();
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).BuyDomik(playerId, typeId);
            uow.Commit();
        }

        private void UpgradeDomik(int playerId, int domikId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).UpgradeDomik(playerId, domikId);
            uow.Commit();
        }

        private Domiki.Web.Business.Models.Order[] GetOrders(int playerId)
        {
            using var uow = GetUow();
            var orders = GetOrderManager(uow).GetOrders(playerId).ToArray();
            uow.Commit();
            return orders;
        }

        private void CompleteOrder(int playerId, int orderId)
        {
            using var uow = GetUow();
            GetOrderManager(uow).CompleteOrder(playerId, orderId);
            uow.Commit();
        }

        private int[] GetCompletedGoalIds(int playerId)
        {
            using var uow = GetUow();
            return uow.Context.PlayerGoals.Where(x => x.PlayerId == playerId).OrderBy(x => x.GoalId).Select(x => x.GoalId).ToArray();
        }

        private PlayerEvent GetGoalEvent(int playerId, int goalId)
        {
            using var uow = GetUow();
            return uow.Context.PlayerEvents.Single(x => x.PlayerId == playerId && x.Type == PlayerEventType.GoalCompleted && x.Data.Contains($"\"goalId\":{goalId}"));
        }

        private int GetResourceValue(int playerId, int typeId)
        {
            using var uow = GetUow();
            return uow.Context.Resources.Single(x => x.PlayerId == playerId && x.TypeId == typeId).Value;
        }

        private int GetDomikId(int playerId, int typeId)
        {
            using var uow = GetUow();
            return uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == typeId).Id;
        }

        private int GetVillageLevel(int playerId)
        {
            using var uow = GetUow();
            return GetVillageLevelCalculator(uow).GetLevel(playerId).Level;
        }

        private void SeedCompletedGoals(int playerId, params int[] goalIds)
        {
            using var uow = GetUow();
            uow.Context.PlayerGoals.AddRange(goalIds.Select(goalId => new PlayerGoal { PlayerId = playerId, GoalId = goalId, CompleteDate = DateTime.UtcNow }));
            uow.Commit();
        }

        private void SetDomikLevel(int playerId, int domikId, int level)
        {
            using var uow = GetUow();
            uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.Id == domikId).Level = level;
            uow.Commit();
        }

        private void GrantBuiltDomik(int playerId, int typeId, int level)
        {
            using var uow = GetUow();
            var id = (uow.Context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0) + 1;
            uow.Context.Domiks.Add(new Domiki.Web.Data.Domik { PlayerId = playerId, Id = id, TypeId = typeId, Level = level });
            uow.Commit();
        }

        private void GrantAllResources(int playerId, int value)
        {
            using var uow = GetUow();
            foreach (var typeId in uow.Context.ResourceTypes.Select(x => x.Id).ToArray())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = typeId };
                    uow.Context.Resources.Add(resource);
                }
                resource.Value += value;
            }
            uow.Commit();
        }

        private void GrantResource(int playerId, int typeId, int value)
        {
            using var uow = GetUow();
            var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
            if (resource == null)
            {
                resource = new Domiki.Web.Data.Resource { PlayerId = playerId, TypeId = typeId };
                uow.Context.Resources.Add(resource);
            }
            resource.Value += value;
            uow.Commit();
        }

        private void GrantReputation(int playerId, int neighborId, int value)
        {
            using var uow = GetUow();
            var reputation = uow.Context.NeighborReputations.SingleOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);
            if (reputation == null)
            {
                reputation = new NeighborReputation { PlayerId = playerId, NeighborId = neighborId };
                uow.Context.NeighborReputations.Add(reputation);
            }
            reputation.Points += value;
            uow.Commit();
        }
    }
}
