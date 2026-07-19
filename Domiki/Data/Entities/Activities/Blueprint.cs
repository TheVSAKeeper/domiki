using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник чертежей: постройка, которую открывает чертёж, и сосед-источник с порогом репутации для выдачи.
/// </summary>
public class Blueprint
{
    /// <summary>
    /// Идентификатор чертежа.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название чертежа.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код чертежа – по нему код находит конкретный чертёж по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Тип постройки, покупка которой становится доступна после получения чертежа.
    /// </summary>
    public int DomikTypeId { get; set; }

    /// <summary>
    /// Сосед, чья репутация выдаёт чертёж по достижении ReputationThreshold.
    /// </summary>
    public int NeighborId { get; set; }

    /// <summary>
    /// Очки репутации с соседом <see cref="NeighborId"/>, при достижении которых чертёж выдаётся игроку автоматически.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.BlueprintManager.EnsureBlueprints"/>.
    /// </remarks>
    public int ReputationThreshold { get; set; }

    /// <summary>
    /// Навигационное свойство к типу постройки, который открывает чертёж.
    /// </summary>
    public DomikType DomikType { get; set; } = null!;

    /// <summary>
    /// Навигационное свойство к соседу-источнику чертежа.
    /// </summary>
    public Neighbor Neighbor { get; set; } = null!;
}
