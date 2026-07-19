using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Порог обжитости деревни, открывающий постройку очередного экземпляра домика этого типа.
/// </summary>
/// <remarks>
/// Нумерация экземпляров с <c>2</c>, <c>3</c> и так далее.
/// </remarks>
[PrimaryKey(nameof(DomikTypeId), nameof(Ordinal))]
public class DomikTypeCountGate
{
    /// <summary>
    /// Часть составного ключа – тип домика, для которого задан порог.
    /// </summary>
    [Column(Order = 1)]
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Часть составного ключа – порядковый номер экземпляра домика этого типа.
    /// </summary>
    /// <remarks>
    /// Нумерация начинается с <c>2</c>, <c>3</c> и так далее – начиная с этого номера действует порог.
    /// </remarks>
    [Column(Order = 2)]
    public int Ordinal { get; set; }

    /// <summary>
    /// Обжитость деревни, необходимая, чтобы построить <see cref="Ordinal"/>-й экземпляр домика этого типа.
    /// </summary>
    public int UnlockLevel { get; set; }
}
