namespace Domiki.Web.Village.Dto;

/// <summary>
/// Погода – глобальное состояние, одно на всех игроков, с прогнозом на сутки вперёд.
/// </summary>
public class WeatherStateDto
{
    /// <summary>
    /// Погода, действующая прямо сейчас.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – период ещё не насеян планировщиком.
    /// </remarks>
    public WeatherPeriodDto Current { get; set; }

    /// <summary>
    /// Ближайшие будущие периоды погоды – геймплей планирования, не рулетка.
    /// </summary>
    public WeatherPeriodDto[] Forecast { get; set; }
}

/// <summary>
/// Один период действия одного вида погоды.
/// </summary>
public class WeatherPeriodDto
{
    /// <summary>
    /// Тип погоды – ссылка на справочник <see cref="Village.Models.WeatherType.Id"/>.
    /// </summary>
    public int WeatherTypeId { get; set; }

    /// <summary>
    /// Отображаемое название погоды.
    /// </summary>
    public string WeatherName { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Момент начала периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент окончания периода.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Влияние погоды на выход производства по типам построек.
    /// </summary>
    /// <remarks>
    /// Постройки без записи в массиве не затронуты.
    /// </remarks>
    public WeatherEffectDto[] Effects { get; set; }
}

/// <summary>
/// Влияние погоды на выход производства одного типа построек.
/// </summary>
public class WeatherEffectDto
{
    /// <summary>
    /// Тип построек, на который действует эффект, – ссылка на <see cref="Core.Dto.DomikTypeDto.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Множитель выхода производства.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – без изменений. Диапазон эффектов ±25–50%.</value>
    /// <remarks>
    /// См. <see cref="Village.WeatherManager.GetOutputPercent"/>.
    /// </remarks>
    public int OutputPercent { get; set; }
}
