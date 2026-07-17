using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник типов толоки: какой ресурс собирают, базовая цель счётчика и вес при случайном выборе следующей инстанции.
/// </summary>
[Table("TolokaTypes")]
public class TolokaType
{
    /// <summary>
    /// Идентификатор типа толоки.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название толоки.
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Технический код типа толоки – по нему код находит конкретную толоку по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public string LogicName { get; set; }

    /// <summary>
    /// Тип ресурса, который игроки сдают в счётчик этой толоки.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Базовая цель счётчика для одного участника.
    /// </summary>
    /// <remarks>
    /// Реальная цель инстанции (<see cref="Toloka.Goal"/>) умножается на число участников предыдущей толоки.
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Вес типа при взвешенном случайном выборе следующей инстанции толоки.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.TolokaManager.PickTolokaType"/>.
    /// </remarks>
    public int RotationWeight { get; set; }

    /// <summary>
    /// Навигационное свойство к типу собираемого ресурса.
    /// </summary>
    public ResourceType ResourceType { get; set; }
}
