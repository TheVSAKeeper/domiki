using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;
using Domiki.Web.Business;
using System.Text.Json;
using PlayerEventType = Domiki.Web.Data.PlayerEventType;

namespace Domiki.Web.Tests
{
    public class AutoRepeatTests : TestBase
    {
        /// <summary>
        /// Автоповтор перезапускает производство циклами, пока хватает сырья, и останавливается, когда сырьё заканчивается.
        /// </summary>
        [Test]
        public void AutoRepeatRerunsUntilResourcesRunOutTest()
        {
            var playerId = GetPlayerId();
            BuyPotteryWithWorker(playerId);
            GrantResource(playerId, 4, 4);

            StartManufacture(playerId, 4, 43, true);

            Assert.That(GetManufactureCount(playerId, 4), Is.Zero);
            Assert.That(GetWorkers(playerId).All(x => x.ManufactureId == null), Is.True);
            Assert.That(GetResourceValue(playerId, 4), Is.Zero);
            Assert.That(GetResourceValue(playerId, 12), Is.EqualTo(2));
        }

        /// <summary>
        /// Несколько циклов автоповтора одного производства схлопываются в одно событие завершения с суммарным выходом.
        /// </summary>
        [Test]
        public void AutoRepeatMergesManufactureFinishedEventsTest()
        {
            var playerId = GetPlayerId();
            BuyPotteryWithWorker(playerId);
            GrantResource(playerId, 4, 4);

            StartManufacture(playerId, 4, 43, true);

            using (var uow = GetUow())
            {
                var events = uow.Context.PlayerEvents.Where(x => x.PlayerId == playerId && !x.Read && x.Type == PlayerEventType.ManufactureFinished).ToList();
                Assert.That(events, Has.Count.EqualTo(1));

                using var data = JsonDocument.Parse(events[0].Data);
                Assert.That(data.RootElement.GetProperty("cycles").GetInt32(), Is.EqualTo(2));
                var resource = data.RootElement.GetProperty("resources").EnumerateArray().Single(x => x.GetProperty("resourceTypeId").GetInt32() == 12);
                Assert.That(resource.GetProperty("value").GetInt32(), Is.EqualTo(2));
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var recap = GetPlayerEventManager(uow).TakeRecap(playerId, DateTimeHelper.GetNowDate());
                Assert.That(recap.Events.Count(x => x.Type == PlayerEventType.ManufactureFinished), Is.EqualTo(1));
                uow.Commit();
            }
        }

        /// <summary>
        /// События завершения производства от построек разного типа не схлопываются между собой, а остаются раздельными.
        /// </summary>
        [Test]
        public void ManufactureFinishedEventsOfDifferentDomikTypesAreNotMergedTest()
        {
            var playerId = GetPlayerId();
            RecordManufactureFinished(playerId, 1);
            RecordManufactureFinished(playerId, 2);

            using (var uow = GetUow())
            {
                var events = uow.Context.PlayerEvents.Where(x => x.PlayerId == playerId && !x.Read && x.Type == PlayerEventType.ManufactureFinished).ToList();
                Assert.That(events, Has.Count.EqualTo(2));
                uow.Commit();
            }
        }

        /// <summary>
        /// Если сырья хватает лишь частично на цикл автоповтора, недостающий ресурс не списывается вовсе.
        /// </summary>
        [Test]
        public void PartialShortageDoesNotWriteOffTest()
        {
            var playerId = GetPlayerId();
            BuyForgeWithWorker(playerId);
            GrantResource(playerId, 17, 2);
            GrantResource(playerId, 7, 1);

            StartManufacture(playerId, 4, 24, true);

            Assert.That(GetManufactureCount(playerId, 4), Is.Zero);
            Assert.That(GetWorkers(playerId).All(x => x.ManufactureId == null), Is.True);
            Assert.That(GetResourceValue(playerId, 17), Is.EqualTo(1));
            Assert.That(GetResourceValue(playerId, 7), Is.Zero);
            Assert.That(GetResourceValue(playerId, 8), Is.EqualTo(1));
        }

        /// <summary>
        /// Завершение автоповторного производства не падает, даже если рецепт больше не привязан к текущему уровню домика.
        /// </summary>
        [Test]
        public void AutoRepeatSurvivesRecipeMissingFromLevelTest()
        {
            var playerId = GetPlayerId();
            GrantDomik(playerId, 4, 1, 3);

            int manufactureId;
            using (var uow = GetUow())
            {
                var manufacture = new Domiki.Web.Data.Manufacture
                {
                    DomikId = 4,
                    DomikPlayerId = playerId,
                    ReceiptId = 22,
                    PlodderCount = 1,
                    FinishDate = DateTimeHelper.GetNowDate().AddSeconds(-3600),
                    DurationSeconds = 1800,
                    OutputPercent = 100,
                    AutoRepeat = true,
                    UseOptional = false,
                };
                uow.Context.Manufactures.Add(manufacture);
                uow.Context.SaveChanges();
                manufactureId = manufacture.Id;
                uow.Commit();
            }

            using (var uow = GetUow())
            {
                var domikManager = GetDomikManager(uow);
                var calcInfo = new CalculateInfo { PlayerId = playerId, ObjectId = manufactureId, Type = CalculateTypes.Manufacture };
                Assert.DoesNotThrow(() => domikManager.FinishManufacture(DateTimeHelper.GetNowDate(), calcInfo));
                uow.Commit();
            }

            Assert.That(GetManufactureCount(playerId, 4), Is.Zero);
            using (var uow = GetUow())
            {
                Assert.That(uow.Context.Manufactures.Any(x => x.Id == manufactureId), Is.False);
                uow.Commit();
            }
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
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik { PlayerId = playerId, Id = 3, TypeId = 2, Level = 1 });
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                {
                    PlayerId = playerId,
                    Id = 4,
                    TypeId = 1,
                    Level = 3,
                });
                uow.Commit();
            }
        }

        private void BuyPotteryWithWorker(int playerId)
        {
            using (var uow = GetUow())
            {
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik { PlayerId = playerId, Id = 3, TypeId = 2, Level = 1 });
                uow.Context.Domiks.Add(new Domiki.Web.Data.Domik
                {
                    PlayerId = playerId,
                    Id = 4,
                    TypeId = 13,
                    Level = 3,
                });
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

        private void RecordManufactureFinished(int playerId, int domikTypeId)
        {
            using (var uow = GetUow())
            {
                GetPlayerEventManager(uow).RecordManufactureFinished(playerId, domikTypeId, new Dictionary<int, int> { { 6, 1 } });
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
                uow.Commit();
            }
        }
    }
}
