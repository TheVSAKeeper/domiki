namespace Domiki.Web.Village.Models;

/// <summary>
/// Текущий сезонный период рейтингов экрана «Мир».
/// </summary>
/// <remarks>
/// Вычисляется в <see cref="SeasonManager.GetCurrentSeason"/> и отдаётся на клиент как <see cref="Dto.SeasonDto"/>.
/// </remarks>
public class Season
{
    /// <summary>
    /// Порядковый номер сезона.
    /// </summary>
    /// <remarks>
    /// Отсчитывается от <see cref="SeasonManager.SeasonEpoch"/> шагами по <see cref="SeasonManager.SeasonDurationSeconds"/>.
    /// </remarks>
    public int Number { get; set; }

    /// <summary>
    /// Момент начала сезона.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент окончания сезона.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// По этому моменту подводятся итоги номинаций (см. <see cref="Dto.WorldVillageDto.SeasonOrders"/>,
    /// <see cref="Dto.WorldVillageDto.SeasonToloka"/> и <see cref="Dto.WorldVillageDto.SeasonExpeditions"/>).
    /// </remarks>
    public DateTime EndDate { get; set; }
}
