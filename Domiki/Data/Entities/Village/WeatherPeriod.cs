using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отрезок расписания погоды, общий для всех игроков.
/// </summary>
/// <remarks>
/// <see cref="Village.WeatherManager"/> держит расписание заполненным на сутки вперёд и ротирует его планировщиком <see cref="Core.Scheduling.Calculator"/>.
/// </remarks>
[Table("WeatherPeriods")]
public class WeatherPeriod
{
    /// <summary>
    /// Идентификатор отрезка погоды.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Тип погоды, действующий в этом отрезке – ссылка на <see cref="WeatherType"/>.
    /// </summary>
    public int WeatherTypeId { get; set; }

    /// <summary>
    /// Момент начала действия погоды.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент окончания действия погоды.
    /// </summary>
    /// <remarks>
    /// Планировщик <see cref="Core.Scheduling.Calculator"/> в этот момент ротирует расписание на следующий отрезок.
    /// </remarks>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Навигационное свойство к типу погоды.
    /// </summary>
    public WeatherType WeatherType { get; set; }
}
