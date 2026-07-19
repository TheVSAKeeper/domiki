using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Прокачка трудяги по конкретному типу домика: число завершённых производств этого типа и производный от него бонус к выходу.
/// </summary>
[Table("WorkerSkills")]
[PrimaryKey(nameof(WorkerId), nameof(DomikTypeId))]
public class WorkerSkill
{
    /// <summary>
    /// Трудяга, которому принадлежит прокачка.
    /// </summary>
    public int WorkerId { get; set; }

    /// <summary>
    /// Тип домика, по которому идёт прокачка.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Сколько раз трудяга завершил производство в домике этого типа.
    /// </summary>
    /// <remarks>
    /// Растёт на <c>1</c> за каждое завершение (см. <see cref="Core.DomikManager.IncrementWorkerSkill"/>); бонус к выходу считается из
    /// этого числа асимптотически (см. <see cref="Workers.WorkerSkillCalculator.GetBonusPercent"/>, потолок <c>15%</c>).
    /// </remarks>
    public int Uses { get; set; }

    /// <summary>
    /// Навигационное свойство к трудяге.
    /// </summary>
    public Worker Worker { get; set; } = null!;
}
