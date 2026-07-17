namespace Domiki.Web.Village.Dto;

/// <summary>
/// Текущий сезонный период рейтингов экрана «Мир».
/// </summary>
public sealed record SeasonDto
{
    /// <summary>
    /// Порядковый номер сезона, начиная с <c>1</c>.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// Момент начала сезона.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Момент окончания сезона.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// По этому моменту подводятся итоги номинаций (см. <see cref="WorldVillageDto.SeasonOrders"/>, <see cref="WorldVillageDto.SeasonToloka"/>
    /// и <see cref="WorldVillageDto.SeasonExpeditions"/>).
    /// </remarks>
    public required DateTime EndDate { get; init; }
}
