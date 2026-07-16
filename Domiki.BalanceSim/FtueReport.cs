using System.Text;

namespace Domiki.BalanceSim;

public sealed class FtueReport
{
    private readonly IReadOnlyList<SimulationRunResult> _runs;

    public FtueReport(IReadOnlyList<SimulationRunResult> runs)
    {
        _runs = runs;
    }

    public string Render()
    {
        var output = new StringBuilder();
        output.AppendLine("FTUE-симулятор: казуальный сценарий, 48 ч, сиды 1–7.");
        output.AppendLine("  Метрика                         Медиана      Min/max       Цель");
        RenderTime(output, "Первое подкрепление", _runs.Select(x => x.FirstIncomeSeconds), 300);
        RenderTime(output, "Первый старт улучшения", _runs.Select(x => x.FirstUpgradeStartSeconds), 2700);
        RenderInt(output, "Действия за первые 15 мин", _runs.Select(x => x.ActionsFirst15Min), value => value >= 8, ">= 8");
        RenderInt(output, "Невыполнимые заказы, день 1", _runs.Select(x => x.InfeasibleOrdersDay1), value => value == 0, "= 0");
        RenderInt(output, "Наказов выполнено за 48 ч", _runs.Select(x => x.GoalsCompleted48h), null, "—");
        return output.ToString().TrimEnd();
    }

    private static void RenderTime(StringBuilder output, string name, IEnumerable<int?> values, int target)
    {
        var all = values.ToArray();
        var median = all.OrderBy(x => x ?? int.MaxValue).ElementAt(all.Length / 2);
        var min = all.Min(x => x ?? int.MaxValue);
        var max = all.Max(x => x ?? int.MaxValue);
        var targetText = $"<= {target} с";
        var status = median != null && median <= target ? "OK" : "FAIL";
        output.AppendLine($"  {name.PadRight(31)}  {FormatTime(median).PadLeft(9)}  {FormatRange(min, max).PadLeft(11)}  {targetText} {status}");
    }

    private static void RenderInt(StringBuilder output, string name, IEnumerable<int> values, Func<int, bool>? success, string targetText)
    {
        var all = values.OrderBy(x => x).ToArray();
        var median = all[all.Length / 2];
        var status = success == null ? string.Empty : (success(median) ? " OK" : " FAIL");
        var range = $"{all[0]}–{all[^1]}";
        output.AppendLine($"  {name.PadRight(31)}  {median,9}  {range.PadLeft(11)}  {targetText}{status}");
    }

    private static string FormatTime(int? seconds)
    {
        return seconds == null ? "не достигнуто" : seconds.Value + " с";
    }

    private static string FormatRange(int min, int max)
    {
        return min == int.MaxValue ? "—" : min == max ? min + " с" : $"{min}–{max} с";
    }
}