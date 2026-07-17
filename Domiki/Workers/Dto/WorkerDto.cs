namespace Domiki.Web.Workers.Dto;

/// <summary>
/// Именованный трудяга игрока.
/// </summary>
/// <remarks>
/// Черта (<see cref="TraitId"/>), состояние усталости/болезни (<see cref="RestUntil"/>, <see cref="SickUntil"/>), занятость
/// (<see cref="ManufactureId"/>, <see cref="ExpeditionId"/>) и наработанные навыки (<see cref="Skills"/>).
/// </remarks>
public class WorkerDto
{
    /// <summary>
    /// Идентификатор трудяги.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя трудяги.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Грамматический род имени трудяги – для склонений в тексте на клиенте.
    /// </summary>
    /// <remarks>
    /// Значение <see cref="Workers.WorkerGender"/>, выводится из <see cref="Name"/> функцией <see cref="Workers.NameGrammar.GenderOf"/>.
    /// </remarks>
    public int Gender { get; set; }

    /// <summary>
    /// Черта трудяги – ссылка на справочник черт.
    /// </summary>
    public int TraitId { get; set; }

    /// <summary>
    /// Отображаемое название черты.
    /// </summary>
    public string TraitName { get; set; }

    /// <summary>
    /// Технический код черты, используется как ключ на клиенте.
    /// </summary>
    public string TraitLogicName { get; set; }

    /// <summary>
    /// Изменение длительности производства от черты в процентах.
    /// </summary>
    /// <value>Проценты.</value>
    /// <remarks>
    /// Отрицательное значение ускоряет производство.
    /// </remarks>
    public int TraitDurationPercent { get; set; }

    /// <summary>
    /// <see langword="true"/> – трудяга с этой чертой не устаёт и не нуждается в отдыхе.
    /// </summary>
    /// <remarks>
    /// При <see langword="true"/> <see cref="RestUntil"/> никогда не выставляется (см. <see cref="Core.DomikManager.FatigueThresholdSeconds"/>).
    /// </remarks>
    public bool NoFatigue { get; set; }

    /// <summary>
    /// <see langword="true"/> – трудяга с этой чертой не подвержен болезни.
    /// </summary>
    /// <remarks>
    /// При <see langword="true"/> <see cref="SickUntil"/> никогда не выставляется (см. <see cref="Core.DomikManager.SickChancePercent"/>).
    /// </remarks>
    public bool NoSick { get; set; }

    /// <summary>
    /// Производство, в котором сейчас занят трудяга – ссылка на <see cref="Core.Dto.ManufactureDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – в производстве не занят.
    /// </remarks>
    public int? ManufactureId { get; set; }

    /// <summary>
    /// Экспедиция, в которой сейчас находится трудяга – ссылка на <see cref="Activities.Dto.ExpeditionDto.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – не в походе.
    /// </remarks>
    public int? ExpeditionId { get; set; }

    /// <summary>
    /// Момент окончания отдыха в бараке.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – трудяга не отдыхает. Выставляется по накоплению <see cref="Core.DomikManager.FatigueThresholdSeconds"/> секунд
    /// работы (см. <see cref="Core.DomikManager.FinishManufacture"/>).
    /// </remarks>
    public DateTime? RestUntil { get; set; }

    /// <summary>
    /// Момент выздоровления.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – трудяга не болен. Шанс заболеть при завершении производства – <see cref="Core.DomikManager.SickChancePercent"/>
    /// (см. <see cref="Core.DomikManager.FinishManufacture"/>).
    /// </remarks>
    public DateTime? SickUntil { get; set; }

    /// <summary>
    /// Наработанные навыки трудяги по типам построек.
    /// </summary>
    public WorkerSkillDto[] Skills { get; set; }
}
