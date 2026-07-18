namespace Domiki.Web.Workers.Models;

/// <summary>
/// Именованный трудяга игрока.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Workers.WorkerManager.GetWorkers"/> и отдаётся на клиент как <see cref="Dto.WorkerDto"/>.
/// </remarks>
public class Worker
{
    /// <summary>
    /// Идентификатор трудяги.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя трудяги.
    /// </summary>
    /// <remarks>
    /// Генерируется при найме (см. <see cref="Workers.WorkerManager.GetWorkerName"/>), уникально в пределах игрока.
    /// </remarks>
    public required string Name { get; set; }

    /// <summary>
    /// Черта трудяги – ссылка на справочник <see cref="Trait"/>.
    /// </summary>
    /// <remarks>
    /// Назначается случайно при найме и может смениться находкой <see cref="Data.Entities.ExpeditionLootKind.TraitUpgrade"/> в экспедиции.
    /// </remarks>
    public required Trait Trait { get; set; }

    /// <summary>
    /// Производство, в котором сейчас занят трудяга – ссылка на <see cref="Core.Models.Manufacture.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга не занят производством (<see cref="Workers.WorkerManager.IsFree"/> проверяет вместе с
    /// <see cref="ExpeditionId"/> и <see cref="RestUntil"/>).
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
    /// Поручение соседа, которым сейчас занят трудяга.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – трудяга не занят поручением (<see cref="Workers.WorkerManager.IsFree"/> проверяет вместе с
    /// <see cref="ManufactureId"/>, <see cref="ExpeditionId"/> и <see cref="RestUntil"/>).
    /// </remarks>
    public int? ErrandId { get; set; }

    /// <summary>
    /// Накопленное время работы без отдыха.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// По достижении порога усталости обнуляется, и трудяга уходит на отдых (<see cref="RestUntil"/>).
    /// </remarks>
    public int WorkedSeconds { get; set; }

    /// <summary>
    /// Момент, до которого трудяга отдыхает и недоступен для назначения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – трудяга не отдыхает. Тем же полем помечается вынужденный отдых при болезни.
    /// </remarks>
    public DateTime? RestUntil { get; set; }

    /// <summary>
    /// Момент, до которого трудяга болен.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – трудяга здоров.
    /// </remarks>
    public DateTime? SickUntil { get; set; }

    /// <summary>
    /// Наработанные навыки трудяги по типам построек.
    /// </summary>
    public WorkerSkill[] Skills { get; set; } = [];
}
