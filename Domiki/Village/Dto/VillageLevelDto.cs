namespace Domiki.Web.Village.Dto;

/// <summary>
/// Обжитость деревни и все связанные с нею открытия.
/// </summary>
public sealed record VillageLevelDto
{
    /// <summary>
    /// Обжитость деревни.
    /// </summary>
    /// <remarks>
    /// <c>Buildings + Residents×2 + Reputation×5 + min(Comfort, 50)</c>. Слагаемые: <see cref="Buildings"/>, <see cref="Residents"/>,
    /// <see cref="Reputation"/>, <see cref="Comfort"/> (см. <see cref="Village.VillageLevelCalculator.ComputeLevel"/>).
    /// </remarks>
    public required int Level { get; init; }

    /// <summary>
    /// Сумма <see cref="Core.Dto.DomikDto.Level"/> всех построек игрока.
    /// </summary>
    public required int Buildings { get; init; }

    /// <summary>
    /// Вместимость барака (число коек) – слагаемое «жители» в <see cref="Level"/>.
    /// </summary>
    public required int Residents { get; init; }

    /// <summary>
    /// Число пройденных вех репутации у соседей.
    /// </summary>
    /// <remarks>
    /// <see cref="Economy.Dto.NeighborReputationDto.Points"/> ÷ <see cref="Village.VillageLevelCalculator.ReputationPointsPerMilestone"/>
    /// у каждого соседа, суммарно.
    /// </remarks>
    public required int Reputation { get; init; }

    /// <summary>
    /// Очки уюта от декора игрока, учтённые в обжитости.
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="DecorStateDto.Comfort"/>; в формуле <see cref="Level"/> ограничено сверху <c>50</c>
    /// (<see cref="Village.VillageLevelCalculator.ComfortHabitabilityCap"/>).
    /// </remarks>
    public required int Comfort { get; init; }

    /// <summary>
    /// Число визитов соседей с последнего большого гостинца.
    /// </summary>
    /// <remarks>
    /// Каждый <c>7</c>-й визит выдаёт большой гостинец декором (см. <see cref="Economy.GiftManager.TryGrantGift"/>).
    /// </remarks>
    public required int VisitsSinceBigGift { get; init; }

    /// <summary>
    /// Все пороги открытий, связанных с обжитостью деревни.
    /// </summary>
    /// <remarks>
    /// Постройки, соседи и прочий контент, гейтящийся обжитостью (<see cref="Level"/>).
    /// </remarks>
    public required VillageLevelUnlockDto[] Unlocks { get; init; }
}

/// <summary>
/// Одно открытие обжитости или иного гейта.
/// </summary>
public sealed record VillageLevelUnlockDto
{
    /// <summary>
    /// Требуемая обжитость деревни (<see cref="VillageLevelDto.Level"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – порог гейтится не уровнем, а чертежом или репутацией (см. <see cref="Requirement"/>).
    /// </remarks>
    public int? Level { get; init; }

    /// <summary>
    /// Название того, что откроется.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Текстовое описание условия, если оно не выражается уровнем.
    /// </summary>
    /// <remarks>
    /// Чертёж или репутация у соседа. <see langword="null"/>, если условие – только <see cref="Level"/>.
    /// </remarks>
    public string? Requirement { get; init; }

    /// <summary>
    /// Открыт ли этот порог текущей обжитостью деревни.
    /// </summary>
    /// <remarks>
    /// Для открытий по чертежам значение остаётся <see langword="false"/>, пока игрок не получит чертёж.
    /// </remarks>
    public required bool Unlocked { get; init; }

    /// <summary>
    /// Вид открытия для отображения в клиентской дорожной карте.
    /// </summary>
    /// <remarks>
    /// <c>building</c> обозначает постройку, <c>neighbor</c> – соседа, <c>feature</c> – игровую механику.
    /// </remarks>
    public required string Kind { get; init; }

    /// <summary>
    /// Техническое имя открытия для выбора иконки и игрового описания на клиенте.
    /// </summary>
    /// <remarks>
    /// Для соседа совпадает с его <c>LogicName</c>, для умной артели равно <c>smart_artel</c>.
    /// </remarks>
    public string? LogicName { get; init; }
}
