using Domiki.Web.Reference;
using System.Globalization;
using System.Text;

namespace Domiki.BalanceSim;

/// <summary>
/// Сравнивает шесть конфигураций уклада деревни (без уклада + пять соседей) по монетам/ТЧ, частоте срабатывания
/// клампа 0.6× вне «Рвения» и времени до чертежа специализации СВОЕГО соседа (GAMEDESIGN_IMPL.md, срез «Уклад
/// деревни» С7) – столбец «Свой чертёж» не годится для сравнения между строками: он не про один и тот же чертёж,
/// а про чертёж, привязанный к принятому в этой строке укладу (у Заречья и строки без уклада своего чертежа нет).
/// </summary>
public sealed class ProfileComparisonReport
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly SimulationData _data;
    private readonly IReadOnlyList<(string Label, int? NeighborId, SimulationReport Report)> _configs;

    /// <param name="data">Загруженные справочники, использованные всеми прогонами.</param>
    /// <param name="configs">Шесть конфигураций: подпись, id соседа-уклада (<see langword="null"/> – без уклада) и итоговый отчёт по трём сценариям.</param>
    public ProfileComparisonReport(SimulationData data, IReadOnlyList<(string Label, int? NeighborId, SimulationReport Report)> configs)
    {
        _data = data;
        _configs = configs;
    }

    /// <summary>
    /// Рендерит сравнительную таблицу по всем трём сценариям.
    /// </summary>
    /// <returns>Текстовый отчёт.</returns>
    public string Render()
    {
        var output = new StringBuilder();
        output.AppendLine("Сравнение укладов деревни (С7, срез 1): шесть конфигураций × три сценария × 7 сидов.");
        foreach (var scenario in new[] { ScenarioKind.Casual, ScenarioKind.Optimal, ScenarioKind.Extreme })
        {
            output.AppendLine();
            output.AppendLine($"Сценарий: {GetScenarioName(scenario)}");
            output.AppendLine("  Уклад             Монеты/ТЧ  Свой чертёж, ч   Кламп вне Рвения");
            foreach (var (label, neighborId, report) in _configs)
            {
                var runs = report.Runs[scenario];
                var coinsPerHour = MedianDouble(runs.Select(CoinsPerWorkerHour));
                var blueprintHours = MedianBlueprintHours(neighborId, runs);
                var clampRate = MedianDouble(runs.Select(ClampFireRate));
                output.AppendLine($"  {label.PadRight(16)}  {coinsPerHour,9:F2}  {FormatHours(blueprintHours),14}   {FormatPercent(clampRate)}");
            }
        }

        return output.ToString().TrimEnd();
    }

    private double CoinsPerWorkerHour(SimulationRunResult result)
    {
        if (result.TotalWorkerSeconds <= 0)
        {
            return 0;
        }

        var totalCoinValue = _data.ResourceTypes
            .Where(type => _data.IsLiquid(type.Id))
            .Sum(type => result.FinalResources.GetValueOrDefault(type.Id) * ResourceManager.GetMarketValue(type.Id));
        return totalCoinValue / (result.TotalWorkerSeconds / 3600.0);
    }

    private static double ClampFireRate(SimulationRunResult result)
    {
        return result.ManufactureStartCount == 0 ? 0 : result.ClampFireCount / (double)result.ManufactureStartCount;
    }

    /// <summary>
    /// Медианное время до чертежа, привязанного к специализации <paramref name="neighborId"/>.
    /// </summary>
    /// <param name="neighborId">Сосед-уклад строки; <see langword="null"/> (строка «без уклада») и Заречье не имеют своего чертежа.</param>
    /// <param name="runs">Прогоны конфигурации по одному сценарию.</param>
    /// <returns>Часы до добычи чертежа или <see langword="null"/>, если чертёж не привязан к укладу либо не достигнут ни в одном прогоне.</returns>
    private double? MedianBlueprintHours(int? neighborId, IReadOnlyList<SimulationRunResult> runs)
    {
        var blueprintId = neighborId is int id ? _data.Blueprints.FirstOrDefault(x => x.NeighborId == id)?.Id : null;
        if (blueprintId == null)
        {
            return null;
        }

        var values = runs.Select(x => x.BlueprintTimes.GetValueOrDefault(blueprintId.Value, -1)).OrderBy(x => x).ToArray();
        var median = values.ElementAt(values.Length / 2);
        return median < 0 ? null : median / 3600.0;
    }

    private static double MedianDouble(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        return ordered.ElementAt(ordered.Length / 2);
    }

    private static string GetScenarioName(ScenarioKind scenario)
    {
        return scenario switch
        {
            ScenarioKind.Casual => "Казуальный",
            ScenarioKind.Optimal => "Оптимальный",
            ScenarioKind.Extreme => "Экстремальный",
            _ => string.Empty,
        };
    }

    private static string FormatHours(double? hours)
    {
        return hours == null ? "—" : hours.Value.ToString("F1", RussianCulture);
    }

    private static string FormatPercent(double value)
    {
        return (value * 100).ToString("F2", RussianCulture) + "%";
    }
}
