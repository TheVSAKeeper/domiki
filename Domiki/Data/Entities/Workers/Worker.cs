using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Трудяга игрока: черта, текущая занятость (производство или экспедиция) и состояние усталости/болезни.
/// </summary>
[Table("Workers")]
public class Worker
{
    /// <summary>
    /// Идентификатор трудяги.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок-владелец трудяги.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Имя трудяги.
    /// </summary>
    /// <remarks>
    /// Генерируется при найме (см. <see cref="Workers.WorkerManager.GetWorkerName"/>), уникально в пределах игрока.
    /// </remarks>
    [MaxLength(100)]
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    /// <summary>
    /// Черта трудяги – ссылка на <see cref="Trait"/>.
    /// </summary>
    /// <remarks>
    /// Назначается случайно при найме и может смениться находкой <see cref="ExpeditionLootKind.TraitUpgrade"/> в экспедиции.
    /// </remarks>
    public int TraitId { get; set; }

    /// <summary>
    /// Производство, в котором сейчас занят трудяга.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга не занят производством (<see cref="Workers.WorkerManager.IsFree"/> проверяет вместе с <see cref="ExpeditionId"/> и <see cref="RestUntil"/>).
    /// </remarks>
    public int? ManufactureId { get; set; }

    /// <summary>
    /// Экспедиция, в которой сейчас участвует трудяга.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга не в походе.
    /// </remarks>
    public int? ExpeditionId { get; set; }

    /// <summary>
    /// Накопленное время работы без отдыха в секундах.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// По достижении порога усталости обнуляется, и трудяга уходит на отдых (<see cref="RestUntil"/>).
    /// </remarks>
    public int WorkedSeconds { get; set; }

    /// <summary>
    /// Момент, до которого трудяга отдыхает и недоступен для назначения.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга не отдыхает. Тем же полем помечается вынужденный отдых при болезни.
    /// </remarks>
    public DateTime? RestUntil { get; set; }

    /// <summary>
    /// Момент, до которого трудяга болен.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга здоров. Влияет на кап одновременно больных трудяг игрока (<see cref="Core.DomikManager.MaxSickPerPlayer"/>) и на иммунитет сразу после выздоровления.
    /// </remarks>
    public DateTime? SickUntil { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку-владельцу.
    /// </summary>
    public Player Player { get; set; }

    /// <summary>
    /// Навигационное свойство к черте трудяги.
    /// </summary>
    public Trait Trait { get; set; }

    /// <summary>
    /// Навигационное свойство к производству, в котором занят трудяга.
    /// </summary>
    public Manufacture Manufacture { get; set; }

    /// <summary>
    /// Навигационное свойство к экспедиции, в которой участвует трудяга.
    /// </summary>
    public Expedition Expedition { get; set; }

    /// <summary>
    /// Прокачка трудяги по типам домиков – накопленное число использований и производный бонус к выходу.
    /// </summary>
    public ICollection<WorkerSkill> Skills { get; set; }
}
