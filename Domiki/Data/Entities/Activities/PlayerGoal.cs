namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отметка выполнения наказа старосты цепочки FTUE.
/// </summary>
/// <remarks>
/// Наличие строки означает, что игрок закрыл этот <see cref="StarterGoal"/> и получил награду.
/// </remarks>
public class PlayerGoal
{
    /// <summary>
    /// Игрок, выполнивший наказ.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Выполненный наказ – ссылка на <see cref="StarterGoal"/>.
    /// </summary>
    public int GoalId { get; set; }

    /// <summary>
    /// Момент выполнения наказа.
    /// </summary>
    public DateTime CompleteDate { get; set; }
}
