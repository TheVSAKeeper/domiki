namespace Domiki.Web.Activities.Dto;

/// <summary>
/// Состояние цепочки стартовых FTUE-целей игрока.
/// </summary>
public class GoalsStateDto
{
    /// <summary>
    /// Текущий наказ цепочки.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – все цели пройдены.
    /// </remarks>
    public ActiveGoalDto Active { get; set; }

    /// <summary>
    /// Сколько наказов цепочки уже выполнено.
    /// </summary>
    /// <remarks>
    /// Растёт до <see cref="TotalCount"/>.
    /// </remarks>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Общая длина цепочки целей.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Оставшиеся заряды усердия.
    /// </summary>
    /// <remarks>
    /// Ускоряют производство рецептов короче <see cref="Core.DomikManager.ZealMaxRecipeSeconds"/> секунд в 2 или 4 раза (×4 – при запасе
    /// свыше <see cref="Core.DomikManager.ZealX4Threshold"/> зарядов), расходуются по одному за использование.
    /// </remarks>
    public int ZealCharges { get; set; }
}

/// <summary>
/// Один наказ цепочки стартовых целей.
/// </summary>
public class ActiveGoalDto
{
    /// <summary>
    /// Идентификатор наказа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Порядковый номер наказа в цепочке.
    /// </summary>
    /// <remarks>
    /// На единицу больше <see cref="GoalsStateDto.CompletedCount"/> в момент выдачи этого наказа.
    /// </remarks>
    public int Ordinal { get; set; }

    /// <summary>
    /// Текст наказа.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Награда монетами за выполнение наказа.
    /// </summary>
    public int RewardCoins { get; set; }
}
