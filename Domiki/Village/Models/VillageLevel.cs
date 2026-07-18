namespace Domiki.Web.Village.Models;

/// <summary>
/// Обжитость деревни и ближайшие ещё не открытые ею пороги.
/// </summary>
/// <remarks>
/// Вычисляется в <see cref="VillageLevelCalculator.GetLevel"/> и отдаётся на клиент как <see cref="Dto.VillageLevelDto"/>.
/// </remarks>
public class VillageLevel
{
    /// <summary>
    /// Обжитость деревни.
    /// </summary>
    /// <remarks>
    /// <c>Buildings + Residents×2 + Reputation×5 + min(Comfort, 50)</c> (см. <see cref="VillageLevelCalculator.ComputeLevel"/>).
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Сумма уровней всех построек игрока – слагаемое «постройки» в <see cref="Level"/>.
    /// </summary>
    public int Buildings { get; set; }

    /// <summary>
    /// Вместимость барака (число коек) – слагаемое «жители» в <see cref="Level"/>.
    /// </summary>
    public int Residents { get; set; }

    /// <summary>
    /// Число пройденных вех репутации у соседей – слагаемое «репутация» в <see cref="Level"/>.
    /// </summary>
    /// <remarks>
    /// Очки репутации у каждого соседа делятся на <see cref="VillageLevelCalculator.ReputationPointsPerMilestone"/>, суммарно.
    /// </remarks>
    public int Reputation { get; set; }

    /// <summary>
    /// Очки уюта от декора игрока, учтённые в обжитости.
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="DecorState.Comfort"/>; в формуле <see cref="Level"/> ограничено сверху
    /// <see cref="VillageLevelCalculator.ComfortHabitabilityCap"/>.
    /// </remarks>
    public int Comfort { get; set; }

    /// <summary>
    /// Число визитов соседей с последнего большого гостинца.
    /// </summary>
    /// <remarks>
    /// Каждый <c>7</c>-й визит выдаёт большой гостинец декором (см. <see cref="Economy.GiftManager.TryGrantGift"/>).
    /// </remarks>
    public int VisitsSinceBigGift { get; set; }

    /// <summary>
    /// Ближайшие ещё не открытые пороги.
    /// </summary>
    /// <remarks>
    /// Постройки, соседи и прочий контент, гейтящийся обжитостью (<see cref="Level"/>).
    /// </remarks>
    public VillageLevelUnlock[] UpcomingUnlocks { get; set; } = [];
}

/// <summary>
/// Один ещё не открытый порог обжитости или иного гейта.
/// </summary>
public class VillageLevelUnlock
{
    /// <summary>
    /// Требуемая обжитость деревни.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – порог гейтится не уровнем, а чертежом или репутацией (см. <see cref="Requirement"/>).
    /// </remarks>
    public int? Level { get; set; }

    /// <summary>
    /// Название того, что откроется.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Текстовое описание условия, если оно не выражается уровнем.
    /// </summary>
    /// <remarks>
    /// Чертёж или репутация у соседа. <see langword="null"/>, если условие – только <see cref="Level"/>.
    /// </remarks>
    public string? Requirement { get; set; }
}
