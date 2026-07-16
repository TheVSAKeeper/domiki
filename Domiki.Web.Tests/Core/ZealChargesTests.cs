using Domiki.Web.Core;
using Domiki.Web.Data.Entities;

namespace Domiki.Web.Tests
{
    public class ZealChargesTests : TestBase
    {
        private const int ClayMineDomikId = 2;
        private const int ClayDigReceiptId = 1;
        private const int ClayDigEightHoursReceiptId = 14;
        private const int MarketTypeId = 7;
        private const int SellClayReceiptId = 6;
        private const int ClayResourceTypeId = 4;

        /// <summary>
        /// Новый игрок получает 24 заряда рвения.
        /// </summary>
        [Test]
        public void NewPlayerStartsWithTwentyFourZealChargesTest()
        {
            Assert.That(GetZealCharges(CreatePlayer()), Is.EqualTo(DomikManager.ZealStartCharges));
        }

        /// <summary>
        /// Копка глины тратит заряд рвения и идёт вчетверо быстрее обычного.
        /// </summary>
        [Test]
        public void ClayDigUsesFourfoldZealSpeedupAndChargeTest()
        {
            var playerId = CreatePlayer();
            SetWorkersToOrdinary(playerId);

            var manufacture = StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId, false);

            Assert.That(manufacture.DurationSeconds, Is.EqualTo(900));
            Assert.That(GetZealCharges(playerId), Is.EqualTo(23));
        }

        /// <summary>
        /// Ускорение копки от рвения ступенчато слабеет по мере расхода зарядов и никогда не уводит их счётчик в отрицательные значения.
        /// </summary>
        /// <param name="initialCharges">Заряды рвения перед запуском.</param>
        /// <param name="expectedDuration">Ожидаемая длительность производства в секундах.</param>
        /// <param name="expectedCharges">Ожидаемый остаток зарядов рвения после запуска.</param>
        [TestCase(17, 900, 16)]
        [TestCase(16, 1800, 15)]
        [TestCase(1, 1800, 0)]
        [TestCase(0, 3600, 0)]
        public void ClayDigAppliesThresholdSpeedupWithoutNegativeChargesTest(int initialCharges, int expectedDuration, int expectedCharges)
        {
            var playerId = CreatePlayer();
            SetWorkersToOrdinary(playerId);
            SetZealCharges(playerId, initialCharges);

            var manufacture = StartManufacture(playerId, ClayMineDomikId, ClayDigReceiptId, false);

            Assert.That(manufacture.DurationSeconds, Is.EqualTo(expectedDuration));
            Assert.That(GetZealCharges(playerId), Is.EqualTo(expectedCharges));
        }

        /// <summary>
        /// Долгий восьмичасовой рецепт копки глины не расходует заряды рвения и не ускоряется ими.
        /// </summary>
        [Test]
        public void EightHourClayDigDoesNotUseZealTest()
        {
            var playerId = CreatePlayer();
            SetWorkersToOrdinary(playerId);

            var manufacture = StartManufacture(playerId, ClayMineDomikId, ClayDigEightHoursReceiptId, false);

            Assert.That(manufacture.DurationSeconds, Is.EqualTo(28800));
            Assert.That(GetZealCharges(playerId), Is.EqualTo(24));
        }

        /// <summary>
        /// Продажа ресурса на рынке не расходует заряды рвения и не ускоряется ими.
        /// </summary>
        [Test]
        public void MarketSaleDoesNotUseZealTest()
        {
            var playerId = CreatePlayer();
            GrantAllResources(playerId, 1000);
            BuyDomik(playerId, MarketTypeId);
            GrantResource(playerId, ClayResourceTypeId, 10);
            SetWorkersToOrdinary(playerId);

            var marketId = GetDomikId(playerId, MarketTypeId);
            var manufacture = StartManufacture(playerId, marketId, SellClayReceiptId, false);

            Assert.That(manufacture.DurationSeconds, Is.EqualTo(60));
            Assert.That(GetZealCharges(playerId), Is.EqualTo(24));
        }

        private int CreatePlayer()
        {
            using var uow = GetUow();
            var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private Manufacture StartManufacture(int playerId, int domikId, int receiptId, bool justFinish)
        {
            using var uow = GetUow();
            GetDomikManager(uow, justFinish).StartManufacture(playerId, domikId, receiptId);
            var manufacture = uow.Context.Manufactures.Single(x => x.DomikPlayerId == playerId && x.DomikId == domikId);
            uow.Commit();
            return manufacture;
        }

        private void BuyDomik(int playerId, int typeId)
        {
            using var uow = GetUow();
            GetDomikManager(uow).BuyDomik(playerId, typeId);
            uow.Commit();
        }

        private void SetWorkersToOrdinary(int playerId)
        {
            using var uow = GetUow();
            GetWorkerManager(uow).EnsureWorkers(playerId);
            uow.Context.WorkerSkills.RemoveRange(uow.Context.WorkerSkills.Where(x => x.Worker.PlayerId == playerId));
            foreach (var worker in uow.Context.Workers.Where(x => x.PlayerId == playerId))
            {
                worker.TraitId = 1;
            }
            uow.Commit();
        }

        private int GetZealCharges(int playerId)
        {
            using var uow = GetUow();
            return uow.Context.Players.Single(x => x.Id == playerId).ZealCharges;
        }

        private void SetZealCharges(int playerId, int value)
        {
            using var uow = GetUow();
            uow.Context.Players.Single(x => x.Id == playerId).ZealCharges = value;
            uow.Commit();
        }

        private int GetDomikId(int playerId, int typeId)
        {
            using var uow = GetUow();
            return uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.TypeId == typeId).Id;
        }

        private void GrantAllResources(int playerId, int value)
        {
            using var uow = GetUow();
            foreach (var typeId in uow.Context.ResourceTypes.Select(x => x.Id).ToArray())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
                if (resource == null)
                {
                    resource = new Resource { PlayerId = playerId, TypeId = typeId };
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
                resource = new Resource { PlayerId = playerId, TypeId = typeId };
                uow.Context.Resources.Add(resource);
            }
            resource.Value += value;
            uow.Commit();
        }
    }
}
