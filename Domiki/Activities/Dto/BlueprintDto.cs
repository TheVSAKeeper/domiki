namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Чертёж постройки следующего круга.
/// </summary>
/// <remarks>
/// Условие получения у конкретного соседа (<see cref="NeighborId"/>) и то, получен ли он игроком уже (<see cref="Owned"/>).
/// </remarks>
public class BlueprintDto
{
    /// <summary>
    /// Идентификатор чертежа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название чертежа.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Постройка, которую открывает чертёж – ссылка на <see cref="Core.Dto.DomikTypeDto.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Сосед, у которого чертёж можно заслужить репутацией или получить в экспедиции.
    /// </summary>
    /// <remarks>
    /// Репутация сравнивается с <see cref="ReputationThreshold"/> (<see cref="Activities.BlueprintManager.EnsureBlueprints"/>); в экспедиции
    /// чертёж может выпасть как редкая добыча (<see cref="ExpeditionLootDto.BlueprintId"/>).
    /// </remarks>
    public int NeighborId { get; set; }

    /// <summary>
    /// Имя соседа-владельца чертежа.
    /// </summary>
    public string NeighborName { get; set; }

    /// <summary>
    /// Порог репутации у соседа, при достижении которого чертёж считается заслуженным.
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="CurrentReputation"/> здесь же и с <see cref="Economy.Dto.NeighborReputationDto.Points"/> в общем списке репутаций
    /// игрока (см. <see cref="Activities.BlueprintManager.EnsureBlueprints"/>).
    /// </remarks>
    public int ReputationThreshold { get; set; }

    /// <summary>
    /// Текущая репутация игрока у этого соседа.
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="Economy.Dto.NeighborReputationDto.Points"/> этого же соседа; сравнивается с <see cref="ReputationThreshold"/>.
    /// </remarks>
    public int CurrentReputation { get; set; }

    /// <summary>
    /// <see langword="true"/> – чертёж уже получен (репутацией или в экспедиции), постройка доступна к покупке.
    /// </summary>
    public bool Owned { get; set; }
}
