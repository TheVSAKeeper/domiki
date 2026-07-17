namespace Domiki.Web.Village.Dto;

/// <summary>
/// Обжитость деревни и ближайшие ещё не открытые ею пороги.
/// </summary>
public class VillageLevelDto
{
    /// <summary>
    /// Обжитость деревни.
    /// </summary>
    /// <remarks>
    /// <c>Buildings + Residents×2 + Reputation×5 + min(Comfort, 50)</c>. Слагаемые: <see cref="Buildings"/>, <see cref="Residents"/>,
    /// <see cref="Reputation"/>, <see cref="Comfort"/> (см. <see cref="Village.VillageLevelCalculator.ComputeLevel"/>).
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Сумма <see cref="Core.Dto.DomikDto.Level"/> всех построек игрока.
    /// </summary>
    public int Buildings { get; set; }

    /// <summary>
    /// Вместимость барака (число коек) – слагаемое «жители» в <see cref="Level"/>.
    /// </summary>
    public int Residents { get; set; }

    /// <summary>
    /// Число пройденных вех репутации у соседей.
    /// </summary>
    /// <remarks>
    /// <see cref="Economy.Dto.NeighborReputationDto.Points"/> ÷ <see cref="Village.VillageLevelCalculator.ReputationPointsPerMilestone"/>
    /// у каждого соседа, суммарно.
    /// </remarks>
    public int Reputation { get; set; }

    /// <summary>
    /// Очки уюта от декора игрока, учтённые в обжитости.
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="DecorStateDto.Comfort"/>; в формуле <see cref="Level"/> ограничено сверху <c>50</c>
    /// (<see cref="Village.VillageLevelCalculator.ComfortHabitabilityCap"/>).
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
    public VillageLevelUnlockDto[] UpcomingUnlocks { get; set; }
}

/// <summary>
/// Один ещё не открытый порог обжитости или иного гейта.
/// </summary>
public class VillageLevelUnlockDto
{
    /// <summary>
    /// Требуемая обжитость деревни (<see cref="VillageLevelDto.Level"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – порог гейтится не уровнем, а чертежом или репутацией (см. <see cref="Requirement"/>).
    /// </remarks>
    public int? Level { get; set; }

    /// <summary>
    /// Название того, что откроется.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Текстовое описание условия, если оно не выражается уровнем.
    /// </summary>
    /// <remarks>
    /// Чертёж или репутация у соседа. <see langword="null"/>, если условие – только <see cref="Level"/>.
    /// </remarks>
    public string Requirement { get; set; }
}
