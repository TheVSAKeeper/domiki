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
    /// Готовая строка позиций корзины сбора, вида «300 Кирпич + 300 Доска».
    /// </summary>
    /// <remarks>
    /// Собрана из позиций <see cref="Data.Entities.TolokaPosition"/> этой инстанции, отсортированных по <see cref="Data.Entities.TolokaPosition.ResourceTypeId"/>,
    /// с резолвом названия ресурса через <see cref="Reference.ResourceManager.GetResourceTypes"/>.
    /// </remarks>
    public required string ResourcesText { get; set; }

    /// <summary>
    /// Порядковый номер сезона, в который толока завершилась.
    /// </summary>
    /// <remarks>
    /// Чистая деривация от <see cref="CompletedDate"/> через <see cref="Village.SeasonManager.GetCurrentSeason"/>,
    /// прошлые даты валидны.
    /// </remarks>
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Число различных игроков, внёсших вклад в эту инстанцию толоки.
    /// </summary>
    /// <remarks>
    /// Distinct-счёт по <see cref="Data.Entities.TolokaContribution.PlayerId"/> с этим <see cref="Data.Entities.Toloka.Id"/> –
    /// вклад пер-ресурсный, без distinct строки задвоятся.
    /// </remarks>
    public int Participants { get; set; }

    /// <summary>
    /// Момент завершения толоки.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime CompletedDate { get; set; }
}
