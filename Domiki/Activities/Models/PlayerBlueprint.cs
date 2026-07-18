using Domiki.Web.Economy.Models;

namespace Domiki.Web.Activities.Models;

/// <summary>
/// Чертёж в контексте конкретного игрока – условие получения у соседа и текущий прогресс.
/// </summary>
/// <remarks>
/// Собирается в <see cref="BlueprintManager.GetBlueprints"/> и отдаётся на клиент как <see cref="Dto.BlueprintDto"/>.
/// </remarks>
public class PlayerBlueprint
{
    /// <summary>
    /// Справочная запись чертежа.
    /// </summary>
    public required Blueprint Blueprint { get; set; }

    /// <summary>
    /// Сосед, у которого чертёж можно заслужить репутацией или получить в экспедиции.
    /// </summary>
    public required Neighbor Neighbor { get; set; }

    /// <summary>
    /// Текущая репутация игрока у этого соседа.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="Blueprint.ReputationThreshold"/>.
    /// </remarks>
    public int CurrentReputation { get; set; }

    /// <summary>
    /// <see langword="true"/> – чертёж уже получен (репутацией или в экспедиции), постройка доступна к покупке.
    /// </summary>
    public bool Owned { get; set; }
}
