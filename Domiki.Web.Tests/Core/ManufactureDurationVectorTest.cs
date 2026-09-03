using Domiki.Web.Core;
using Domiki.Web.Core.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domiki.Web.Tests;

public sealed class ManufactureDurationVectorTest
{
    private static readonly JsonSerializerOptions VectorJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = true,
    };

    /// <summary>
    /// Черта трудяги ускоряет смену, а навык сокращает её сверх черты.
    /// </summary>
    [Test]
    public void WorkerTraitAndSkillShortenShiftTest()
    {
        const int baseSeconds = 10000;

        var plain = Compute(baseSeconds, [0], [0]);
        var quick = Compute(baseSeconds, [-20], [0]);
        var skilled = Compute(baseSeconds, [-20], [10]);

        Assert.Multiple(() =>
        {
            Assert.That(plain.Seconds, Is.EqualTo(baseSeconds));
            Assert.That(quick.Seconds, Is.EqualTo(8000));
            Assert.That(skilled.Seconds, Is.EqualTo(7200));
        });
    }

    /// <summary>
    /// Смена не опускается ниже 60 % базовой длительности рецепта, сколько бы сокращений ни сошлось.
    /// </summary>
    [Test]
    public void DurationFloorHoldsAtSixtyPercentTest()
    {
        const int baseSeconds = 10000;

        var result = Compute(baseSeconds, [-30], [30], profilePercent: 80, perkPercent: 90);

        Assert.That(result.Seconds, Is.EqualTo((int)(baseSeconds * 0.6)));
    }

    /// <summary>
    /// Рвение ускоряет короткую смену вдвое, а при запасе свыше 16 зарядов – вчетверо, и списывает заряд.
    /// </summary>
    /// <param name="zealCharges">Запас зарядов рвения.</param>
    /// <param name="expectedSeconds">Ожидаемая длительность смены.</param>
    /// <param name="expectedSpent">Заряд списан.</param>
    [TestCase(0, 3600, false)]
    [TestCase(1, 1800, true)]
    [TestCase(17, 900, true)]
    public void ZealSpeedsUpShortShiftTest(int zealCharges, int expectedSeconds, bool expectedSpent)
    {
        var result = Compute(3600, [0], [0], zealCharges: zealCharges);

        Assert.Multiple(() =>
        {
            Assert.That(result.Seconds, Is.EqualTo(expectedSeconds));
            Assert.That(result.ZealSpent, Is.EqualTo(expectedSpent));
        });
    }

    /// <summary>
    /// Рвение не берётся ни за смену длиннее часа, ни за Лавку.
    /// </summary>
    /// <param name="receiptDurationSeconds">Базовая длительность рецепта.</param>
    /// <param name="isMarketDomik">Смена идёт в Лавке.</param>
    [TestCase(3601, false)]
    [TestCase(3600, true)]
    public void ZealSkipsLongShiftAndMarketTest(int receiptDurationSeconds, bool isMarketDomik)
    {
        var result = Compute(receiptDurationSeconds, [0], [0], zealCharges: 24, isMarketDomik: isMarketDomik);

        Assert.Multiple(() =>
        {
            Assert.That(result.Seconds, Is.EqualTo(receiptDurationSeconds));
            Assert.That(result.ZealSpent, Is.False);
        });
    }

    /// <summary>
    /// Файл случаев, по которому клиент сверяет свой расчёт длительности, совпадает с расчётом бэкенда: разошедшийся
    /// файл значит, что нарисованный до ответа сервера таймер показывает игроку не ту секунду.
    /// </summary>
    /// <remarks>
    /// Перезаписать файл после правки формулы – тестом <see cref="RewriteVectorsTest"/>.
    /// </remarks>
    [Test]
    public void ClientVectorsMatchCalculatorTest()
    {
        Assert.That(File.ReadAllText(VectorPath()).ReplaceLineEndings("\n"), Is.EqualTo(BuildVectorSource()));
    }

    /// <summary>
    /// Перезаписывает файл случаев для клиента по текущей формуле длительности.
    /// </summary>
    [Test]
    [Explicit]
    public void RewriteVectorsTest()
    {
        File.WriteAllText(VectorPath(), BuildVectorSource(), new UTF8Encoding(false));
    }

    private static ManufactureDurationResult Compute(
        int receiptDurationSeconds,
        int[] traitDurationPercents,
        int[] skillBonusPercents,
        int profilePercent = 100,
        int perkPercent = 100,
        bool isMarketDomik = false,
        int zealCharges = 0)
    {
        return ManufactureDurationCalculator.Compute(new()
        {
            ReceiptDurationSeconds = receiptDurationSeconds,
            TraitDurationPercents = traitDurationPercents,
            SkillBonusPercents = skillBonusPercents,
            ProfilePercent = profilePercent,
            PerkPercent = perkPercent,
            IsMarketDomik = isMarketDomik,
            ZealCharges = zealCharges,
        });
    }

    private static ManufactureDurationInput[] GetVectorInputs()
    {
        return
        [
            Input(600, [0], [0]),
            Input(600, [-20], [0]),
            Input(600, [15], [0]),
            Input(600, [0], [12]),
            Input(3607, [-20, 0, 15], [3, 7, 0]),
            Input(3607, [-20, -10], [5, 5], profilePercent: 85),
            Input(3607, [0], [0], perkPercent: 90),
            Input(3607, [-30, -30], [30, 30], profilePercent: 80, perkPercent: 90),
            Input(1000, [-33], [7], profilePercent: 93, perkPercent: 95),
            Input(3600, [0], [0], zealCharges: 1),
            Input(3600, [0], [0], zealCharges: 17),
            Input(3601, [0], [0], zealCharges: 17),
            Input(3600, [0], [0], isMarketDomik: true, zealCharges: 17),
            Input(61, [-20, -20, -20], [0, 0, 0], zealCharges: 17),
            Input(1, [0], [0], zealCharges: 17),
            Input(86400, [-20, -10, 0, 15], [10, 0, 5, 0], profilePercent: 85, perkPercent: 90),
        ];
    }

    private static ManufactureDurationInput Input(
        int receiptDurationSeconds,
        int[] traitDurationPercents,
        int[] skillBonusPercents,
        int profilePercent = 100,
        int perkPercent = 100,
        bool isMarketDomik = false,
        int zealCharges = 0)
    {
        return new()
        {
            ReceiptDurationSeconds = receiptDurationSeconds,
            TraitDurationPercents = traitDurationPercents,
            SkillBonusPercents = skillBonusPercents,
            ProfilePercent = profilePercent,
            PerkPercent = perkPercent,
            IsMarketDomik = isMarketDomik,
            ZealCharges = zealCharges,
        };
    }

    private static string BuildVectorSource()
    {
        var vectors = GetVectorInputs()
            .Select(input => new
            {
                Input = input,
                Expected = ManufactureDurationCalculator.Compute(input),
            })
            .ToArray();

        return JsonSerializer.Serialize(vectors, VectorJson).ReplaceLineEndings("\n") + "\n";
    }

    private static string VectorPath()
    {
        return Path.Combine(GetRepositoryRoot(), "Domiki", "ClientApp", "src", "utils", "manufactureDurationVectors.json");
    }

    private static string GetRepositoryRoot([CallerFilePath] string callerPath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(callerPath) ?? AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Domiki.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Не найден корень репозитория – каталог с Domiki.sln");
    }
}
