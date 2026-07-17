namespace Domiki.Web.Village.Dto;

/// <summary>
/// Погода – глобальное состояние, одно на всех игроков, с прогнозом на сутки вперёд.
/// </summary>
public sealed record WeatherStateDto
{
    /// <summary>
    /// Погода, действующая прямо сейчас.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – период ещё не насеян планировщиком.
    /// </remarks>
    public required WeatherPeriodDto Current { get; init; }

    /// <summary>
    /// Ближайшие будущие периоды погоды – геймплей планирования, не рулетка.
    /// </summary>
    public required WeatherPeriodDto[] Forecast { get; init; }
}

/// <summary>
/// Один период действия одного вида погоды.
/// </summary>
public sealed record WeatherPeriodDto
{
    /// <summary>
    /// Тип погоды – ссылка на справочник <see cref="Village.Models.WeatherType.Id"/>.
    /// </summary>
    public required int WeatherTypeId { get; init; }

    /// <summary>
    /// Отображаемое название погоды.
    /// </summary>
    public required string WeatherName { get; init; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Момент начала периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Момент окончания периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Влияние погоды на выход производства по типам построек.
    /// </summary>
    /// <remarks>
    /// Постройки без записи в массиве не затронуты.
    /// </remarks>
    public required WeatherEffectDto[] Effects { get; init; }
}

/// <summary>
/// Влияние погоды на выход производства одного типа построек.
/// </summary>
public sealed record WeatherEffectDto
{
    /// <summary>
    /// Тип построек, на который действует эффект, – ссылка на <see cref="Core.Dto.DomikTypeDto.Id"/>.
    /// </summary>
    public required int DomikTypeId { get; init; }

    /// <summary>
    /// Множитель выхода производства.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – без изменений. Диапазон эффектов ±25–50%.</value>
    /// <remarks>
    /// См. <see cref="Village.WeatherManager.GetOutputPercent"/>.
    /// </remarks>
    public required int OutputPercent { get; init; }
}
