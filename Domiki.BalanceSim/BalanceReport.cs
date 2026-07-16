using Domiki.Web.Reference;
using System.Globalization;
using System.Text;

namespace Domiki.BalanceSim;

public sealed class BalanceReport
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly SimulationData _data;
    private readonly SimulationReport _report;

    public BalanceReport(SimulationData data, SimulationReport report)
    {
        _data = data;
        _report = report;
    }

    public string Render()
    {
        var output = new StringBuilder();
        output.AppendLine($"Баланс-симулятор: домиков {_data.DomikTypes.Length}, рецептов {_data.Receipts.Length}, соседей {_data.Neighbors.Length}, экспедиций {_data.ExpeditionTypes.Length}");
        output.AppendLine("Горизонт: 45 суток, по 7 прогонов на сценарий, сиды 1–7.");
        foreach (var scenario in new[] { ScenarioKind.Casual, ScenarioKind.Optimal, ScenarioKind.Extreme })
        {
            RenderScenario(output, scenario);
        }

        RenderDiagnostics(output);
        return output.ToString().TrimEnd();
    }

    private void RenderScenario(StringBuilder output, ScenarioKind scenario)
    {
        var runs = _report.Runs[scenario];
        output.AppendLine();
        output.AppendLine($"Сценарий: {GetScenarioName(scenario)}");
        output.AppendLine("Обжитость");
        output.AppendLine("  Уровень  Время, ч");
        var maxLevel = MedianInt(runs.Select(x => x.MaxVillageLevel));
        for (var level = 1; level <= maxLevel; level++)
        {
            output.AppendLine($"  {level,7}  {FormatTime(MedianTime(runs.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1))))}");
        }

        output.AppendLine("Постройки");
        output.AppendLine("  Тип                         Первая, ч  Макс. уровень, ч");
        foreach (var type in _data.DomikTypes)
        {
            var first = MedianTime(runs.Select(x => x.FirstDomikTimes.GetValueOrDefault(type.Id, -1)));
            var maximum = MedianTime(runs.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            output.AppendLine($"  {type.Name.PadRight(26)}  {FormatTime(first).PadLeft(9)}  {FormatTime(maximum).PadLeft(16)}");
        }

        output.AppendLine("Соседи и чертежи");
        output.AppendLine("  Веха                        Время, ч");
        foreach (var neighbor in _data.Neighbors)
        {
            var time = MedianTime(runs.Select(x => x.NeighborOpenTimes.GetValueOrDefault(neighbor.Id, -1)));
            output.AppendLine($"  {("Сосед " + neighbor.Name).PadRight(26)}  {FormatTime(time).PadLeft(9)}");
        }

        foreach (var blueprint in _data.Blueprints)
        {
            var time = MedianTime(runs.Select(x => x.BlueprintTimes.GetValueOrDefault(blueprint.Id, -1)));
            output.AppendLine($"  {("Чертёж " + blueprint.Name).PadRight(26)}  {FormatTime(time).PadLeft(9)}");
        }

        output.AppendLine($"Трудяги: простой {FormatPercent(Median(runs.Select(x => x.IdleShare)))}; отдых {FormatPercent(Median(runs.Select(x => x.RestShare)))}.");
        output.AppendLine("Финальные стоки");
        output.AppendLine("  Ресурс                      Кол-во  Монетный эквивалент");
        foreach (var resourceType in _data.ResourceTypes)
        {
            var value = MedianInt(runs.Select(x => x.FinalResources.GetValueOrDefault(resourceType.Id)));
            var coins = value * ResourceManager.GetMarketValue(resourceType.Id);
            output.AppendLine($"  {resourceType.Name.PadRight(26)}  {value,6}  {coins,20}");
        }

        var total = MedianInt(runs.Select(x => x.FinalResources.Sum(resource => resource.Value * ResourceManager.GetMarketValue(resource.Key))));
        output.AppendLine($"  {"Итого".PadRight(26)}  {string.Empty,6}  {total,20}");
        output.AppendLine($"Весь контент выкачан: {FormatTime(MedianTime(runs.Select(x => x.ContentCompleteTime ?? -1)))}.");
    }

    private void RenderDiagnostics(StringBuilder output)
    {
        var casual = _report.Runs[ScenarioKind.Casual];
        var extreme = _report.Runs[ScenarioKind.Extreme];
        output.AppendLine();
        output.AppendLine("Диагностика §8.7");
        output.AppendLine("  Веха                                      Экстремал/казуал");
        foreach (var level in new[] { 8, 20 })
        {
            var casualTime = MedianTime(casual.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1)));
            var extremeTime = MedianTime(extreme.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1)));
            output.AppendLine($"  {("Обжитость " + level).PadRight(40)}  {FormatRatio(extremeTime, casualTime)}");
        }

        foreach (var type in _data.DomikTypes.Where(x => x.MaxLevel > 1))
        {
            var casualTime = MedianTime(casual.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            var extremeTime = MedianTime(extreme.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            output.AppendLine($"  {(type.Name + ", макс. уровень").PadRight(40)}  {FormatRatio(extremeTime, casualTime)}");
        }

        var stalled = casual.Where(x => x.LongestStallSeconds > 24 * 60 * 60).ToArray();
        if (stalled.Length == 0)
        {
            output.AppendLine("  Казуальный: достижимых действий не терял более чем на 24 ч.");
            return;
        }

        var longest = stalled.Max(x => x.LongestStallSeconds);
        output.AppendLine($"  ПРЕДУПРЕЖДЕНИЕ: казуальный стоял более 24 ч; максимум {FormatHours(longest)} ч, сиды {string.Join(", ", stalled.Select(x => x.Seed))}.");
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

    private static int? MedianTime(IEnumerable<int> values)
    {
        var value = values.Select(x => x < 0 ? int.MaxValue : x).OrderBy(x => x).ElementAt(3);
        return value == int.MaxValue ? null : value;
    }

    private static int MedianInt(IEnumerable<int> values)
    {
        return values.OrderBy(x => x).ElementAt(3);
    }

    private static double Median(IEnumerable<double> values)
    {
        return values.OrderBy(x => x).ElementAt(3);
    }

    private static string FormatTime(int? seconds)
    {
        return seconds == null ? "не достигнут" : FormatHours(seconds.Value);
    }

    private static string FormatHours(int seconds)
    {
        return (seconds / 3600.0).ToString("F1", RussianCulture);
    }

    private static string FormatPercent(double value)
    {
        return (value * 100).ToString("F1", RussianCulture) + "%";
    }

    private static string FormatRatio(int? numerator, int? denominator)
    {
        if (numerator == null || denominator == null || denominator == 0)
        {
            return "не достигнут";
        }

        return (numerator.Value / (double)denominator.Value).ToString("F2", RussianCulture) + "×";
    }
}
