using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Происшествие с пропавшим в походе трудягой или загадкой в постройке и последующими поисками.
/// </summary>
/// <remarks>
/// Создаётся при возвращении отряда либо завершении улучшения постройки и остаётся в истории после <see cref="ResolvedDate"/>.
/// Тип похода или постройки хранится снимком, поскольку запись похода к этому моменту удалена.
/// </remarks>
public class Incident
{
    /// <summary>
    /// Идентификатор происшествия.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, у которого произошло происшествие.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Источник завязки происшествия.
    /// </summary>
    public IncidentSourceType SourceType { get; set; }

    /// <summary>
    /// Трудяга, задержавшийся в походе.
    /// </summary>
    /// <remarks>
    /// Снимок без навигации и внешнего ключа: запись живёт в истории после <see cref="ResolvedDate"/> вечно, а обратный указатель <see cref="Worker.IncidentId"/> образовал бы цикл внешних ключей.
    /// </remarks>
    /// <remarks>
    /// <see langword="null"/> – происшествие связано с постройкой.
    /// </remarks>
    public int? MissingWorkerId { get; set; }

    /// <summary>
    /// Идентификатор типа похода, в котором задержался трудяга.
    /// </summary>
    /// <remarks>
    /// Снимок без навигации и внешнего ключа: сама запись похода удаляется в <see cref="Activities.ExpeditionManager.FinishExpedition"/>.
    /// </remarks>
    /// <remarks>
    /// <see langword="null"/> – происшествие связано с постройкой.
    /// </remarks>
    public int? ExpeditionTypeId { get; set; }

    /// <summary>
    /// Идентификатор типа постройки, в которой возникла загадка.
    /// </summary>
    /// <remarks>
    /// Снимок без навигации и внешнего ключа; <see langword="null"/> – происшествие связано с походом.
    /// </remarks>
    public int? DomikTypeId { get; set; }

    /// <summary>
    /// Индекс клиентского шаблона текста происшествия.
    /// </summary>
    /// <remarks>
    /// Диапазон 0..<see cref="Activities.IncidentManager.IncidentTemplateCount"/> - 1; тексты находятся на клиенте.
    /// </remarks>
    public int TemplateId { get; set; }

    /// <summary>
    /// Момент создания происшествия.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime CreateDate { get; set; }

    /// <summary>
    /// Выбранная игроком зацепка, задающая длительность поисков.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public int? ClueId { get; set; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поиски ещё не начаты.
    /// </remarks>
    public DateTime? SearchEndDate { get; set; }

    /// <summary>
    /// Момент развязки происшествия.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – происшествие активно.
    /// </remarks>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Игрок, у которого произошло происшествие.
    /// </summary>
    public Player Player { get; set; } = null!;
}
