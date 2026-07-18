namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Одна запись летописи завершённых толок для экрана «Мир».
/// </summary>
public sealed record TolokaArtifactDto
{
    /// <summary>
    /// Отображаемое название типа толоки.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Готовая строка позиций корзины сбора, вида «300 Кирпич + 300 Доска».
    /// </summary>
    public required string ResourcesText { get; init; }

    /// <summary>
    /// Порядковый номер сезона, в который толока завершилась.
    /// </summary>
    /// <remarks>
    /// Единица прибавлена для отображения, как и в <see cref="Village.Dto.SeasonDto.Number"/>.
    /// </remarks>
    public required int SeasonNumber { get; init; }

    /// <summary>
    /// Число игроков, внёсших вклад в эту инстанцию толоки.
    /// </summary>
    public required int Participants { get; init; }

    /// <summary>
    /// Момент завершения толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime CompletedDate { get; init; }
}
