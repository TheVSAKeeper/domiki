using Domiki.Web.Business;
using Microsoft.Extensions.Logging.Abstractions;

namespace Domiki.Web.Tests
{
    public class CalculatorResilienceTests
    {
        private sealed class PoisonCalculator : Calculator
        {
            private readonly int _poisonObjectId;
            public int GoodProcessed;

            public PoisonCalculator(int poisonObjectId)
                : base(null!, NullLogger<Calculator>.Instance)
            {
                _poisonObjectId = poisonObjectId;
            }

            protected override bool ProcessEvent(DateTime date, CalculateInfo calcDate)
            {
                if (calcDate.ObjectId == _poisonObjectId)
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }

                GoodProcessed++;
                return true;
            }
        }

        /// <summary>
        /// Событие планировщика, падающее с исключением, откладывается на будущее, а не блокирует обработку остальной очереди.
        /// </summary>
        [Test]
        public void PoisonEventDoesNotBlockQueueOrKillSchedulerTest()
        {
            var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var poison = new CalculateInfo { PlayerId = 1, ObjectId = 100, Type = CalculateTypes.Manufacture, Date = baseDate };
            var good = new CalculateInfo { PlayerId = 1, ObjectId = 200, Type = CalculateTypes.Domiks, Date = baseDate.AddSeconds(1) };

            var calc = new PoisonCalculator(poisonObjectId: 100);
            calc.SeedForTest(new[] { poison, good });

            var now = baseDate.AddSeconds(10);
            for (var i = 0; i < 12; i++)
            {
                Assert.DoesNotThrow(() => calc.RunDue(now));
            }

            Assert.That(calc.GoodProcessed, Is.EqualTo(1), "событие за ядовитым обработано, очередь не заблокирована");
            Assert.That(calc.PendingForTest.Any(x => x.ObjectId == 200), Is.False, "обработанное событие убрано из очереди");

            var deferred = calc.PendingForTest.SingleOrDefault(x => x.ObjectId == 100);
            Assert.That(deferred, Is.Not.Null, "ядовитое событие отложено, а не потеряно");
            Assert.That(deferred!.Date, Is.GreaterThan(now), "ядовитое событие перенесено в будущее для повторной попытки");
        }

        /// <summary>
        /// Планировщик за один проход обрабатывает все уже наступившие события и не трогает события из будущего.
        /// </summary>
        [Test]
        public void DrainProcessesAllDueEventsInOnePassTest()
        {
            var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var due1 = new CalculateInfo { ObjectId = 1, Type = CalculateTypes.Manufacture, Date = baseDate };
            var due2 = new CalculateInfo { ObjectId = 2, Type = CalculateTypes.Manufacture, Date = baseDate.AddSeconds(1) };
            var due3 = new CalculateInfo { ObjectId = 3, Type = CalculateTypes.Manufacture, Date = baseDate.AddSeconds(2) };
            var future = new CalculateInfo { ObjectId = 4, Type = CalculateTypes.Manufacture, Date = baseDate.AddSeconds(1000) };

            var calc = new PoisonCalculator(poisonObjectId: -1);
            calc.SeedForTest(new[] { due1, due2, due3, future });

            calc.DrainDue(baseDate.AddSeconds(10), budget: 100);

            Assert.That(calc.GoodProcessed, Is.EqualTo(3), "все наступившие события обработаны за один проход");
            Assert.That(calc.PendingForTest.Select(x => x.ObjectId), Is.EquivalentTo(new[] { 4 }), "будущее событие осталось нетронутым");
        }

        /// <summary>
        /// Бюджет ограничивает число событий, обрабатываемых планировщиком за один проход, остальные ждут следующего тика.
        /// </summary>
        [Test]
        public void DrainStopsAtBudgetTest()
        {
            var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var events = Enumerable.Range(1, 5)
                .Select(i => new CalculateInfo { ObjectId = i, Type = CalculateTypes.Manufacture, Date = baseDate.AddSeconds(i) })
                .ToArray();

            var calc = new PoisonCalculator(poisonObjectId: -1);
            calc.SeedForTest(events);

            calc.DrainDue(baseDate.AddSeconds(100), budget: 2);

            Assert.That(calc.GoodProcessed, Is.EqualTo(2), "бюджет ограничивает число событий за проход");
            Assert.That(calc.PendingForTest, Has.Count.EqualTo(3), "остальные ждут следующего тика");
        }
    }
}
