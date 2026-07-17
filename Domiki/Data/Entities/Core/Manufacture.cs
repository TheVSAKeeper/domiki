using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Запущенное производство в домике: рецепт, занятые трудяги, срок готовности и параметры выхода.
/// </summary>
[Table("Manufactures")]
public class Manufacture
{
    /// <summary>
    /// Идентификатор производства.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Часть составного FK на <see cref="Domik"/> – номер домика, в котором идёт производство.
    /// </summary>
    public int DomikId { get; set; }

    /// <summary>
    /// Часть составного FK на <see cref="Domik"/> – игрок-владелец домика.
    /// </summary>
    /// <remarks>
    /// Продублирован здесь, чтобы искать производства сразу по игроку, не джойня <see cref="Domik"/>.
    /// </remarks>
    public int DomikPlayerId { get; set; }

    /// <summary>
    /// Рецепт, по которому идёт производство.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Сколько трудяг занято в этом производстве.
    /// </summary>
    public int PlodderCount { get; set; }

    /// <summary>
    /// Домик, в котором идёт производство.
    /// </summary>
    public Domik Domik { get; set; }

    /// <summary>
    /// Момент, когда планировщик <see cref="Core.Scheduling.Calculator"/> должен завершить производство и выдать ресурсы.
    /// </summary>
    public DateTime FinishDate { get; set; }

    /// <summary>
    /// Фактическая длительность производства в секундах.
    /// </summary>
    /// <remarks>
    /// Учитывает скорость трудяг и стартовый бонус «нетронутые залежи».
    /// </remarks>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Множитель выхода ресурсов в процентах.
    /// </summary>
    /// <value>Проценты, где <c>100</c> – базовое значение.</value>
    /// <remarks>
    /// Зависит от погоды, толоки и использования опционального ингредиента.
    /// </remarks>
    public int OutputPercent { get; set; } = 100;

    /// <summary>
    /// Если <see langword="true"/> – по завершении производство перезапускается автоматически с теми же параметрами.
    /// </summary>
    public bool AutoRepeat { get; set; }

    /// <summary>
    /// Использован ли опциональный ингредиент рецепта.
    /// </summary>
    /// <remarks>
    /// Даёт бонус к выходу (<see cref="Receipt.OutputBonusPercent"/>), но требует лишний ресурс.
    /// </remarks>
    public bool UseOptional { get; set; }

    /// <summary>
    /// Шанс в процентах, что каждый занятый трудяга заболеет по завершении производства.
    /// </summary>
    /// <value>Проценты.</value>
    /// <remarks>
    /// Следствие плохой погоды.
    /// </remarks>
    public int SickChance { get; set; }
}
