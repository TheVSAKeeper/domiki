using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class InstaFinishTests : TestBase
    {
        private const int GoldResourceTypeId = 5;

        [Test]
        public void HurryManufactureInCapFinishesAndWritesOffGoldTest()
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            GrantResource(playerId, GoldResourceTypeId, 3);
            SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddMinutes(40));

            HurryManufacture(playerId, manufactureId);

            var resources = GetResources(playerId);
            Assert.That(ResourceValue(resources, GoldResourceTypeId), Is.EqualTo(2));
            Assert.That(ResourceValue(resources, 4), Is.EqualTo(1));
            Assert.That(GetDomiks(playerId).Single(x => x.Id == 2).Manufactures, Is.Null.Or.Empty);
            Assert.That(GetWorkers(playerId).Single().ManufactureId, Is.Null);
        }

        [TestCase(40 * 60, 1)]
        [TestCase(3 * 3600 + 60, 4)]
        [TestCase(6 * 3600, 6)]
        public void HurryManufactureCostCeilsRemainingTimeTest(int remainingSeconds, int expectedCost)
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            GrantResource(playerId, GoldResourceTypeId, 6);
            SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddSeconds(remainingSeconds));

            HurryManufacture(playerId, manufactureId);

            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(6 - expectedCost));
        }

        [Test]
        public void HurryManufactureOverCapThrowsAndKeepsStateTest()
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            GrantResource(playerId, GoldResourceTypeId, 10);
            SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddHours(6).AddSeconds(1));

            var ex = Assert.Throws<BusinessException>(() => HurryManufacture(playerId, manufactureId));

            Assert.That(ex.Message, Is.EqualTo("До конца ещё далеко"));
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(10));
            Assert.That(GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }

        [Test]
        public void HurryManufactureWithoutGoldThrowsAndKeepsStateTest()
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            GrantResource(playerId, GoldResourceTypeId, 1);
            SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddHours(2));

            var ex = Assert.Throws<BusinessException>(() => HurryManufacture(playerId, manufactureId));

            Assert.That(ex.Message, Is.EqualTo("Недостаточно Золото"));
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(1));
            Assert.That(GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single().Id, Is.EqualTo(manufactureId));
        }

        [Test]
        public void HurryManufactureMissingOrForeignThrowsTest()
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            var otherPlayerId = GetPlayerId();

            Assert.Throws<BusinessException>(() => HurryManufacture(playerId, int.MaxValue));
            Assert.Throws<BusinessException>(() => HurryManufacture(otherPlayerId, manufactureId));
        }

        [Test]
        public void HurryDomikInCapFinishesAndWritesOffGoldTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 5, false);
            GrantResource(playerId, GoldResourceTypeId, 3);
            SetDomikUpgradeFinish(playerId, 1, DateTimeHelper.GetNowDate().AddHours(2));

            HurryDomik(playerId, 1);

            var domik = GetDomiks(playerId).Single();
            Assert.That(domik.Level, Is.EqualTo(1));
            Assert.That(domik.FinishDate, Is.Null);
            Assert.That(ResourceValue(GetResources(playerId), GoldResourceTypeId), Is.EqualTo(1));
        }

        [Test]
        public void HurryDomikNotUpgradingThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 5);

            var ex = Assert.Throws<BusinessException>(() => HurryDomik(playerId, 1));

            Assert.That(ex.Message, Is.EqualTo("Домик не улучшается"));
        }

        [Test]
        public void HurryDomikMissingOrForeignThrowsTest()
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 5, false);
            var otherPlayerId = GetPlayerId();

            Assert.Throws<BusinessException>(() => HurryDomik(playerId, int.MaxValue));
            Assert.Throws<BusinessException>(() => HurryDomik(otherPlayerId, 1));
        }

        [Test]
        public void HurryManufactureUsesFixedOutputPercentTest()
        {
            var playerId = CreatePlayerWithManufacture(out var manufactureId);
            GrantResource(playerId, GoldResourceTypeId, 1);
            SetManufactureFinish(manufactureId, DateTimeHelper.GetNowDate().AddMinutes(10), 200);

            HurryManufacture(playerId, manufactureId);

            Assert.That(ResourceValue(GetResources(playerId), 4), Is.EqualTo(2));
        }

        private int CreatePlayerWithManufacture(out int manufactureId)
        {
            var playerId = GetPlayerId();
            BuyDomik(playerId, 2);
            BuyDomik(playerId, 5);
            StartManufacture(playerId, 2, 1, false);
            manufactureId = GetDomiks(playerId).Single(x => x.Id == 2).Manufactures.Single().Id;
            return playerId;
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

        private Resource[] GetResources(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var resources = domikManager.GetResources(playerId).ToArray();
                uow.Commit();
                return resources;
            }
        }

        private Domik[] GetDomiks(int playerId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var domiks = domikManager.GetDomiks(playerId).ToArray();
                uow.Commit();
                return domiks;
            }
        }

        private Worker[] GetWorkers(int playerId)
        {
            using (var uow = GetUow())
            {
                var workerManager = GetWorkerManager(uow);
                var workers = workerManager.GetWorkers(playerId).ToArray();
                uow.Commit();
                return workers;
            }
        }

        private void BuyDomik(int playerId, int domikTypeId, bool calculatorJustFinishMode = true)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode);
                domikManager.BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId, bool calculatorJustFinishMode)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, calculatorJustFinishMode);
                domikManager.StartManufacture(playerId, domikId, receiptId);
                uow.Commit();
            }
        }

        private void HurryManufacture(int playerId, int manufactureId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, false);
                domikManager.HurryManufacture(playerId, manufactureId);
                uow.Commit();
            }
        }

        private void HurryDomik(int playerId, int domikId)
        {
            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow, false);
                domikManager.HurryDomik(playerId, domikId);
                uow.Commit();
            }
        }

        private void GrantResource(int playerId, int resourceTypeId, int value)
        {
            using (var uow = GetUow())
            {
                var resource = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == resourceTypeId);
                if (resource == null)
                {
                    resource = new Domiki.Web.Data.Resource
                    {
                        PlayerId = playerId,
                        TypeId = resourceTypeId,
                    };
                    uow.Context.Resources.Add(resource);
                }

                resource.Value += value;
                uow.Context.SaveChanges();
                uow.Commit();
            }
        }

        private void SetManufactureFinish(int manufactureId, DateTime finishDate, int? outputPercent = null)
        {
            using (var uow = GetUow())
            {
                var manufacture = uow.Context.Manufactures.Single(x => x.Id == manufactureId);
                manufacture.FinishDate = finishDate;
                if (outputPercent != null)
                {
                    manufacture.OutputPercent = outputPercent.Value;
                }
                uow.Commit();
            }
        }

        private void SetDomikUpgradeFinish(int playerId, int domikId, DateTime finishDate)
        {
            using (var uow = GetUow())
            {
                var domik = uow.Context.Domiks.Single(x => x.PlayerId == playerId && x.Id == domikId);
                Assert.That(domik.UpgradeSeconds, Is.Not.Null);
                domik.UpgradeCalculateDate = finishDate.AddSeconds(-domik.UpgradeSeconds!.Value);
                uow.Commit();
            }
        }

        private int ResourceValue(Resource[] resources, int typeId)
        {
            return resources.FirstOrDefault(x => x.Type.Id == typeId)?.Value ?? 0;
        }
    }
}
