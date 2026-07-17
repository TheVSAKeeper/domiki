namespace Domiki.Web.Workers.Dto;

/// <summary>
/// Профессиональный опыт трудяги в одном типе построек.
/// </summary>
public class WorkerSkillDto
{
    /// <summary>
    /// Тип построек, в котором накоплен опыт, – ссылка на <see cref="Core.Dto.DomikTypeDto.Id"/>.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Число завершённых производств этого типа, накопивших опыт.
    /// </summary>
    /// <remarks>
    /// Растёт на <c>1</c> за каждое завершение (см. <see cref="Core.DomikManager.IncrementWorkerSkill"/>); определяет
    /// <see cref="BonusPercent"/> (см. <see cref="Workers.WorkerSkillCalculator.GetBonusPercent"/>).
    /// </remarks>
    public int Uses { get; set; }

    /// <summary>
    /// Ускорение производства от опыта в процентах.
    /// </summary>
    /// <value>Проценты, убывающая отдача с капом <c>15%</c>.</value>
    /// <remarks>
    /// Вычисляется из <see cref="Uses"/> функцией <see cref="Workers.WorkerSkillCalculator.GetBonusPercent"/>.
    /// </remarks>
    public int BonusPercent { get; set; }
}
