namespace Domiki.Web.Core.Scheduling;

/// <summary>
/// Тип события в очереди планировщика <see cref="Calculator"/>.
/// </summary>
/// <remarks>
/// Определяет, какой менеджер обработает событие в <see cref="CalculatorTick.Calculate"/>.
/// </remarks>
public enum CalculateTypes
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Завершение улучшения домика.
    /// </summary>
    Domiks = 1,

    /// <summary>
    /// Завершение цикла производства.
    /// </summary>
    Manufacture = 2,

    /// <summary>
    /// Истечение заказа на доске.
    /// </summary>
    OrderExpire = 3,

    /// <summary>
    /// Смена периода погоды.
    /// </summary>
    WeatherRotation = 4,

    /// <summary>
    /// Возвращение отряда из экспедиции.
    /// </summary>
    Expedition = 5,

    /// <summary>
    /// Истечение лота на Торговом дворе.
    /// </summary>
    TradeLotExpire = 6,

    /// <summary>
    /// Истечение офферной фазы поручения соседа или завершение поисков по принятому поручению.
    /// </summary>
    /// <remarks>
    /// Обрабатывается <see cref="Economy.ErrandManager.FinishErrand"/>.
    /// </remarks>
    Errand = 7,

    /// <summary>
    /// Самостоятельное возвращение пропавшего трудяги или завершение поисков по происшествию.
    /// </summary>
    /// <remarks>
    /// Обрабатывается <see cref="Activities.IncidentManager.FinishIncident"/>.
    /// </remarks>
    Incident = 8,
}
