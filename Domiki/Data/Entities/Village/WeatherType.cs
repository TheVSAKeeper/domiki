using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов погоды и её вес при случайном выборе очередного отрезка расписания.
/// </summary>
public class WeatherType
{
    /// <summary>
    /// Идентификатор типа погоды.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название погоды.
    /// </summary>
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код типа погоды – по нему код находит конкретную погоду по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Вес типа при взвешенном случайном выборе следующего отрезка расписания.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Village.WeatherManager.PickWeatherType"/>.
    /// </remarks>
    public int RotationWeight { get; set; }
}
