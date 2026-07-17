using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник наказов старосты – скриптованной цепочки целей первой сессии (FTUE).
/// </summary>
/// <remarks>
/// По одному активному наказу за раз, по возрастанию <see cref="Ordinal"/>.
/// </remarks>
public class StarterGoal
{
    /// <summary>
    /// Идентификатор наказа.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Порядковый номер наказа в цепочке.
    /// </summary>
    /// <remarks>
    /// Активен наказ с наименьшим <see cref="Ordinal"/> среди ещё не выполненных игроком.
    /// </remarks>
    public int Ordinal { get; set; }

    /// <summary>
    /// Текст наказа, показываемый игроку.
    /// </summary>
    [MaxLength(200)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// Условие, закрывающее наказ.
    /// </summary>
    public GoalConditionType ConditionType { get; set; }

    /// <summary>
    /// Первый параметр условия <see cref="ConditionType"/>.
    /// </summary>
    /// <remarks>
    /// Смысл зависит от типа условия, например, требуемый уровень домика или длительность рецепта.
    /// </remarks>
    public int Param { get; set; }

    /// <summary>
    /// Второй параметр условия <see cref="ConditionType"/>.
    /// </summary>
    /// <remarks>
    /// Используется только <see cref="GoalConditionType.UpgradeDomikToLevel"/> – тип домика (<c>0</c> = любой).
    /// </remarks>
    public int Param2 { get; set; }

    /// <summary>
    /// Награда монетами за выполнение наказа.
    /// </summary>
    /// <value>Монеты.</value>
    public int RewardCoins { get; set; }
}
