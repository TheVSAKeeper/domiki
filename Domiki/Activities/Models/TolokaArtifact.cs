namespace Domiki.Web.Activities.Models;

/// <summary>
/// Одна запись летописи завершённых толок для экрана «Мир».
/// </summary>
/// <remarks>
/// Собирается в <see cref="TolokaManager.GetArtifacts"/> из завершённых инстанций <see cref="Data.Entities.Toloka"/>
/// и отдаётся на клиент как <see cref="Dto.TolokaArtifactDto"/>.
/// </remarks>
public class TolokaArtifact
{
    /// <summary>
    /// Отображаемое название типа толоки.
    /// </summary>
    /// <remarks>
    /// См. <see cref="TolokaType.Name"/>.
    /// </remarks>
    public required string Name { get; set; }

    /// <summary>
    /// Название ресурса, который собирали участники.
    /// </summary>
    /// <remarks>
    /// Резолв <see cref="TolokaType.ResourceTypeId"/> через <see cref="Reference.ResourceManager.GetResourceTypes"/>.
    /// </remarks>
    public required string ResourceName { get; set; }

    /// <summary>
    /// Целевое значение счётчика этой инстанции толоки.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Data.Entities.Toloka.Goal"/>.
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Порядковый номер сезона, в который толока завершилась.
    /// </summary>
    /// <remarks>
    /// Чистая деривация от <see cref="CompletedDate"/> через <see cref="Village.SeasonManager.GetCurrentSeason"/>,
    /// прошлые даты валидны.
    /// </remarks>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Число игроков, внёсших вклад в эту инстанцию толоки.
    /// </summary>
    /// <remarks>
    /// Счёт по строкам <see cref="Data.Entities.TolokaContribution"/> с этим <see cref="Data.Entities.Toloka.Id"/>.
    /// </remarks>
    public int Participants { get; set; }

    /// <summary>
    /// Момент завершения толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime CompletedDate { get; set; }
}
