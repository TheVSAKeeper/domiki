using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Поручение соседа – квест-оффер на доске заказов, оплачиваемый временем трудяг.
/// </summary>
/// <remarks>
/// Проходит две фазы: оффер (<see cref="AcceptDate"/> == <see langword="null"/>, живёт до <see cref="ExpireDate"/>)
/// и поиски после принятия (до <see cref="FinishDate"/>). После развязки запись не удаляется, а помечается
/// <see cref="ResolvedDate"/> – остаётся историей для витрины «Пока вас не было».
/// </remarks>
public class Errand
{
    /// <summary>
    /// Идентификатор поручения.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, которому предложено поручение.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Сосед, приславший поручение.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Индекс клиентского шаблона текста поручения.
    /// </summary>
    /// <remarks>
    /// Диапазон 0..<see cref="Economy.ErrandManager.ErrandTemplateCount"/> - 1; сами тексты хранятся на клиенте.
    /// </remarks>
    public int TemplateId { get; set; }

    /// <summary>
    /// Момент, когда офферная фаза поручения истекает, если игрок его не принял.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Планировщик <see cref="Core.Scheduling.Calculator"/> удаляет непринятое поручение по достижении этой даты.
    /// </remarks>
    public DateTime ExpireDate { get; set; }

    /// <summary>
    /// Момент принятия поручения игроком.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё в офферной фазе.
    /// </remarks>
    public DateTime? AcceptDate { get; set; }

    /// <summary>
    /// Выбранная игроком зацепка, задающая длительность поисков.
    /// </summary>
    /// <remarks>
    /// Индекс в <see cref="Economy.ErrandManager.ClueDurationHours"/> и <see cref="Economy.ErrandManager.ClueReputation"/>.
    /// <see langword="null"/> – зацепка ещё не выбрана (поручение не принято).
    /// </remarks>
    public int? ClueId { get; set; }

    /// <summary>
    /// Момент завершения поисков.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение ещё не принято.
    /// </remarks>
    public DateTime? FinishDate { get; set; }

    /// <summary>
    /// Момент развязки поручения.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/> – поручение активно (оффер или поиски). Запись не удаляется после развязки.
    /// </remarks>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Игрок, которому принадлежит поручение.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к соседу из <see cref="NeighborId"/>.
    /// </summary>
    public Neighbor Neighbor { get; set; } = null!;
}
