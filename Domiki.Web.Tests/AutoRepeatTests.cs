using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.Web.Tests
{
    public class AutoRepeatTests : TestBase
    {
        [Test]
        public void AutoRepeatRerunsUntilResourcesRunOutTest()
        {
            var playerId = GetPlayerId();
            BuyForgeWithWorker(playerId);
            GrantResource(playerId, 4, 4);

            StartManufacture(playerId, 2, 22, true);

            Assert.That(GetManufactureCount(playerId, 2), Is.Zero);
            Assert.That(GetWorkers(playerId).All(x => x.ManufactureId == null), Is.True);
            Assert.That(GetResourceValue(playerId, 4), Is.Zero);
            Assert.That(GetResourceValue(playerId, 6), Is.EqualTo(2));
        }

        [Test]
        public void PartialShortageDoesNotWriteOffTest()
        {
            var playerId = GetPlayerId();
            BuyForgeWithWorker(playerId);
            GrantResource(playerId, 6, 2);
            GrantResource(playerId, 7, 1);

            StartManufacture(playerId, 2, 24, true);

            Assert.That(GetManufactureCount(playerId, 2), Is.Zero);
            Assert.That(GetWorkers(playerId).All(x => x.ManufactureId == null), Is.True);
            Assert.That(GetResourceValue(playerId, 6), Is.EqualTo(1));
            Assert.That(GetResourceValue(playerId, 7), Is.Zero);
            Assert.That(GetResourceValue(playerId, 8), Is.EqualTo(1));
        }

        private int GetPlayerId()
        {
            using (var uow = GetUow())
            {
                var playerId = GetDomikManager(uow).GetPlayerId("testUser_" + Guid.NewGuid());
                uow.Commit();
                return playerId;
            }
        }

        private void BuyForgeWithWorker(int playerId)
        {
            BuyDomik(playerId, 2);
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                {
                    PlayerId = playerId,
                    Id = 2,
                    TypeId = 1,
                    Level = 3,
                });
                uow.Commit();
            }
        }

        private void BuyDomik(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                GetDomikManager(uow).BuyDomik(playerId, domikTypeId);
                uow.Commit();
            }
        }

        private void StartManufacture(int playerId, int domikId, int receiptId, bool autoRepeat)
        {
            using (var uow = GetUow())
            {
                GetDomikManager(uow).StartManufacture(playerId, domikId, receiptId, autoRepeat: autoRepeat);
                uow.Commit();
            }
        }

        private int GetManufactureCount(int playerId, int domikId)
        {
            using (var uow = GetUow())
            {
                var count = uow.Context.Manufactures.Count(x => x.DomikPlayerId == playerId && x.DomikId == domikId);
                uow.Commit();
                return count;
            }
        }

        private Worker[] GetWorkers(int playerId)
        {
            using (var uow = GetUow())
            {
                var workers = GetWorkerManager(uow).GetWorkers(playerId).ToArray();
                uow.Commit();
                return workers;
            }
        }

        private int GetResourceValue(int playerId, int resourceTypeId)
        {
            using (var uow = GetUow())
            {
                var value = GetDomikManager(uow).GetResources(playerId).Single(x => x.Type.Id == resourceTypeId).Value;
                uow.Commit();
                return value;
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
                uow.Commit();
            }
        }
    }
}
