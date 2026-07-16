using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class MealTests : TestBase
    {
        private const int BreadResourceTypeId = 15;
        private const int ClayMineDomikId = 2;
        private const int ClayDig8HoursReceiptId = 14;
        private const int ClayDig24HoursReceiptId = 18;
        private const int OrdinaryTraitId = 1;
        private const int SonyaTraitId = 4;

        /// <summary>
        /// Кормление хлебом при включённой опции и наличии запаса сокращает отдых уставшего трудяги вдвое ценой одного хлеба; без опции или без хлеба отдых остаётся полным, а хлеб не тратится.
        /// </summary>
        /// <param name="feedWorkers">Включена ли опция кормления трудяг.</param>
        /// <param name="bread">Сколько хлеба выдано игроку перед стартом.</param>
        /// <param name="expectedRestSeconds">Ожидаемая длительность отдыха трудяги.</param>
        /// <param name="expectedBread">Ожидаемый остаток хлеба после завершения производства.</param>
        [TestCase(true, 3, 3600, 2)]
        [TestCase(false, 3, 7200, 3)]
        [TestCase(true, 0, 7200, 0)]
        public void FatiguedWorkerMealDependsOnFeedSettingAndBreadTest(bool feedWorkers, int bread, int expectedRestSeconds, int expectedBread)
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, OrdinaryTraitId);
            SetFeedWorkers(playerId, feedWorkers);
            if (bread > 0)
            {
                GrantResource(playerId, BreadResourceTypeId, bread);
            }

            StartManufacture(playerId, ClayMineDomikId, ClayDig8HoursReceiptId);
            var manufacture = GetManufacture(playerId);
            var finishDate = manufacture.FinishDate.AddSeconds(1);
            FinishManufacture(playerId, manufacture.Id, finishDate);

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.RestUntil, Is.Not.Null);
            Assert.That((worker.RestUntil!.Value - finishDate).TotalSeconds, Is.EqualTo(expectedRestSeconds).Within(2));
            Assert.That(GetResource(playerId, BreadResourceTypeId), Is.EqualTo(expectedBread));
        }

        /// <summary>
        /// Трудяга с чертой «Соня» не устаёт и не ест хлеб при завершении производства, хлеб остаётся нетронутым.
        /// </summary>
        [Test]
        public void SonyaDoesNotEatWhenFinishingManufactureTest()
        {
            var playerId = GetPlayerId();
            var worker = GetWorkers(playerId).Single();
            SetWorkerTrait(worker.Id, SonyaTraitId);
            GrantResource(playerId, BreadResourceTypeId, 3);

            StartManufacture(playerId, ClayMineDomikId, ClayDig24HoursReceiptId);
            var manufacture = GetManufacture(playerId);
            FinishManufacture(playerId, manufacture.Id, manufacture.FinishDate.AddSeconds(1));

            worker = GetWorkers(playerId).Single();
            Assert.That(worker.RestUntil, Is.Null);
            Assert.That(GetResource(playerId, BreadResourceTypeId), Is.EqualTo(3));
        }

        private int GetPlayerId()
        {
            using var uow = GetUow();
            var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
            uow.Commit();
            return playerId;
        }

        private Worker[] GetWorkers(int playerId)
        {
            using var uow = GetUow();
            var workers = GetWorkerManager(uow).GetWorkers(playerId).ToArray();
            uow.Commit();
            return workers;
        }

        private Manufacture GetManufacture(int playerId)
        {
            using var uow = GetUow();
            var manufacture = GetDomikManager(uow).GetDomiks(playerId).Single(x => x.Id == ClayMineDomikId).Manufactures.Single();
            uow.Commit();
            return manufacture;
        }

        private void SetFeedWorkers(int playerId, bool enabled)
        {
            using var uow = GetUow();
            GetDomikManager(uow).SetFeedWorkers(playerId, enabled);
            uow.Commit();
        }

        private int GetResource(int playerId, int typeId)
        {
            using var uow = GetUow();
            var value = uow.Context.Resources.SingleOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId)?.Value ?? 0;
            uow.Commit();
            return value;
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

        private void SetWorkerTrait(int workerId, int traitId)
        {
            using var uow = GetUow();
            uow.Context.Workers.Single(x => x.Id == workerId).TraitId = traitId;
            uow.Commit();
        }

        private void StartManufacture(int playerId, int domikId, int receiptId)
        {
            using var uow = GetUow();
            GetDomikManager(uow, calculatorJustFinishMode: false).StartManufacture(playerId, domikId, receiptId);
            uow.Commit();
        }

        private void FinishManufacture(int playerId, int manufactureId, DateTime date)
        {
            using var uow = GetUow();
            var result = GetDomikManager(uow).FinishManufacture(date, new CalculateInfo
            {
                PlayerId = playerId,
                ObjectId = manufactureId,
                Date = date,
                Type = CalculateTypes.Manufacture,
            });
            Assert.That(result, Is.True);
            uow.Commit();
        }
    }
}
