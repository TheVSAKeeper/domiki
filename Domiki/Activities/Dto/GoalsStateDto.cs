namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Состояние цепочки стартовых FTUE-целей игрока.
/// </summary>
public sealed record GoalsStateDto
{
    /// <summary>
    /// Текущий наказ цепочки.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – все цели пройдены.
    /// </remarks>
    public required ActiveGoalDto Active { get; init; }

    /// <summary>
    /// Сколько наказов цепочки уже выполнено.
    /// </summary>
    /// <remarks>
    /// Растёт до <see cref="TotalCount"/>.
    /// </remarks>
    public required int CompletedCount { get; init; }

    /// <summary>
    /// Общая длина цепочки целей.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Оставшиеся заряды усердия.
    /// </summary>
    /// <remarks>
    /// Ускоряют производство рецептов короче <see cref="Core.DomikManager.ZealMaxRecipeSeconds"/> секунд в 2 или 4 раза (×4 – при запасе
    /// свыше <see cref="Core.DomikManager.ZealX4Threshold"/> зарядов), расходуются по одному за использование.
    /// </remarks>
    public required int ZealCharges { get; init; }
}

/// <summary>
/// Один наказ цепочки стартовых целей.
/// </summary>
public sealed record ActiveGoalDto
{
    /// <summary>
    /// Идентификатор наказа.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Порядковый номер наказа в цепочке.
    /// </summary>
    /// <remarks>
    /// На единицу больше <see cref="GoalsStateDto.CompletedCount"/> в момент выдачи этого наказа.
    /// </remarks>
    public required int Ordinal { get; init; }

    /// <summary>
    /// Текст наказа.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Награда монетами за выполнение наказа.
    /// </summary>
    public required int RewardCoins { get; init; }
}
