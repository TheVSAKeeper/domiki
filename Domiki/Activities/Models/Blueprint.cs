namespace Domiki.Web.Activities.Models;

/// <summary>
/// Справочная запись чертежа постройки следующего круга.
/// </summary>
/// <remarks>
/// Загружается из БД целиком (см. <see cref="Reference.ResourceManager.GetBlueprints"/>) и оборачивается для игрока в
/// <see cref="PlayerBlueprint"/>; на клиент отдаётся как <see cref="Dto.BlueprintDto"/>.
/// </remarks>
public class Blueprint
{
    /// <summary>
    /// Идентификатор чертежа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название чертежа.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя чертежа.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Постройка, которую открывает чертёж – ссылка на <see cref="Core.Models.DomikType.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Сосед, у которого чертёж можно заслужить репутацией или получить в экспедиции.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Порог репутации у соседа, при достижении которого чертёж считается заслуженным.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Data.Entities.NeighborReputation.Points"/> (см. <see cref="BlueprintManager.EnsureBlueprints"/>).
    /// </remarks>
    public int ReputationThreshold { get; set; }
}
