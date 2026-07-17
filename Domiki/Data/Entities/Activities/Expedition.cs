using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отряд трудяг игрока, отправленный в поход за лутом.
/// </summary>
/// <remarks>
/// Удаляется по завершении (см. <see cref="Activities.ExpeditionManager.FinishExpedition"/>).
/// </remarks>
[Table("Expeditions")]
public class Expedition
{
    /// <summary>
    /// Идентификатор экспедиции.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Игрок, отправивший отряд в поход.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Тип похода – ссылка на <see cref="ExpeditionType"/>.
    /// </summary>
    public int ExpeditionTypeId { get; set; }

    /// <summary>
    /// Момент отправки отряда.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент, когда планировщик <see cref="Core.Scheduling.Calculator"/> должен обсчитать возвращение отряда и выдать лут.
    /// </summary>
    public DateTime FinishDate { get; set; }

    /// <summary>
    /// Отряд взял с собой опциональное снаряжение (провизию).
    /// </summary>
    /// <remarks>
    /// По возвращении трудяги не уходят на отдых от усталости похода.
    /// </remarks>
    public bool Provisioned { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку-владельцу отряда.
    /// </summary>
    public Player Player { get; set; }

    /// <summary>
    /// Навигационное свойство к типу похода.
    /// </summary>
    public ExpeditionType ExpeditionType { get; set; }
}
