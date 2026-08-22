namespace Domiki.Web.Infrastructure;

/// <summary>
/// Выбирает сюжетный шаблон происшествия или поручения с оглядкой на недавнюю историю игрока.
/// </summary>
/// <remarks>
/// Равномерный ролл по пулу из шести даёт первый повтор в среднем на четвёртом событии, и игрок читает это как
/// «сюжеты одинаковые». Исключение недавних шаблонов растягивает интервал, не трогая ни размер пула, ни частоту
/// предложений.
/// </remarks>
public static class TemplatePicker
{
    /// <summary>
    /// Сколько последних шаблонов исключается из выбора.
    /// </summary>
    /// <param name="templateCount">Размер пула шаблонов.</param>
    /// <returns>Половина пула: повтор не ближе, чем через <paramref name="templateCount"/> / 2 + 1 событий.</returns>
    public static int AvoidCount(int templateCount)
    {
        return templateCount / 2;
    }

    /// <summary>
    /// Выбирает шаблон, не попавший в недавнюю историю игрока.
    /// </summary>
    /// <param name="templateCount">Размер пула шаблонов.</param>
    /// <param name="recentTemplateIds">Недавно выпадавшие шаблоны – столько последних, сколько вернул <see cref="AvoidCount"/>.</param>
    /// <returns>Индекс шаблона в диапазоне 0..<paramref name="templateCount"/> - 1.</returns>
    /// <remarks>
    /// Если история покрыла весь пул (шаблонов меньше, чем окно исключения), выбор идёт по всему пулу – игрок
    /// получает сюжет, а не отказ.
    /// </remarks>
    public static int Pick(int templateCount, IReadOnlyCollection<int> recentTemplateIds)
    {
        var candidates = Enumerable.Range(0, templateCount)
            .Where(x => !recentTemplateIds.Contains(x))
            .ToArray();

        return candidates.Length == 0
            ? Random.Shared.Next(templateCount)
            : candidates[Random.Shared.Next(candidates.Length)];
    }
}
