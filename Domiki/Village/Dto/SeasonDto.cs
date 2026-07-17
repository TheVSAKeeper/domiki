namespace Domiki.Web.Village.Dto;

/// <summary>
/// Текущий сезонный период рейтингов экрана «Мир».
/// </summary>
public class SeasonDto
{
    /// <summary>
    /// Порядковый номер сезона, начиная с <c>1</c>.
    /// </summary>
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
    /// По этому моменту подводятся итоги номинаций (см. <see cref="WorldVillageDto.SeasonOrders"/>, <see cref="WorldVillageDto.SeasonToloka"/>
    /// и <see cref="WorldVillageDto.SeasonExpeditions"/>).
    /// </remarks>
    public DateTime EndDate { get; set; }
}
