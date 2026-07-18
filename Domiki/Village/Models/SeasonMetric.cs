namespace Domiki.Web.Village.Models;

/// <summary>
/// Метрика, по которой копится сезонный счётчик игрока для номинаций экрана «Мир».
/// </summary>
/// <remarks>
/// Копится в <see cref="SeasonManager.IncrementCounter"/>, читается в <see cref="SeasonManager.GetCounters"/>
/// и отдаётся на клиент через <see cref="Dto.WorldVillageDto"/>.
/// </remarks>
public enum SeasonMetric
{
    /// <summary>
    /// Метрика не задана.
    /// </summary>
    None = 0,

    /// <summary>
    /// Число выполненных заказов – номинация «Лучший поставщик».
    /// </summary>
    /// <remarks>
    /// См. <see cref="Dto.WorldVillageDto.SeasonOrders"/>.
    /// </remarks>
    Orders = 1,

    /// <summary>
    /// Вклад в толоку – номинация «Герой толоки».
    /// </summary>
    /// <remarks>
    /// См. <see cref="Dto.WorldVillageDto.SeasonToloka"/>.
    /// </remarks>
    Toloka = 2,

    /// <summary>
    /// Число завершённых экспедиций – номинация «Дальние странники».
    /// </summary>
    /// <remarks>
    /// См. <see cref="Dto.WorldVillageDto.SeasonExpeditions"/>.
    /// </remarks>
    Expeditions = 3,
}
