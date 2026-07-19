using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочная позиция корзины сбора типа толоки: какой ресурс и в каком базовом количестве нужен на одного участника.
/// </summary>
[Table("TolokaTypePositions")]
[PrimaryKey(nameof(TolokaTypeId), nameof(ResourceTypeId))]
public class TolokaTypePosition
{
    /// <summary>
    /// Часть составного ключа – тип толоки, чью корзину описывает строка.
    /// </summary>
    [Column(Order = 1)]
    public int TolokaTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – тип ресурса позиции.
    /// </summary>
    [Column(Order = 2)]
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Базовая цель позиции на одного участника.
    /// </summary>
    /// <remarks>
    /// Реальная цель инстанции (<see cref="TolokaPosition.Goal"/>) умножается на число участников предыдущей толоки.
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Навигационное свойство к типу толоки.
    /// </summary>
    public TolokaType TolokaType { get; set; } = null!;
}
