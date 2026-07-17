using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Справочник черт трудяг – каждому новому трудяге случайно назначается одна черта, задающая его особенности в производстве, отдыхе, здоровье и удаче.
/// </summary>
[Table("Traits")]
public class Trait
{
    /// <summary>
    /// Идентификатор черты.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название черты.
    /// </summary>
    [MaxLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// Технический код черты – по нему код находит конкретную черту по смыслу, а не по <see cref="Id"/>.
    /// </summary>
    /// <remarks>
    /// Например, <c>ordinary</c> – обычный трудяга, единственный, которому меняют черту при находке <see cref="ExpeditionLootKind.TraitUpgrade"/>.
    /// </remarks>
    [MaxLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string LogicName { get; set; }

    /// <summary>
    /// Изменение длительности производства в процентах.
    /// </summary>
    /// <value>Проценты; отрицательное значение – трудяга работает быстрее обычного, положительное – медленнее.</value>
    public int DurationPercent { get; set; }

    /// <summary>
    /// Трудяга с этой чертой не устаёт и не уходит на отдых (<see cref="Worker.WorkedSeconds"/> не растёт).
    /// </summary>
    /// <remarks>
    /// Также блокирует заболевание.
    /// </remarks>
    public bool NoFatigue { get; set; }

    /// <summary>
    /// Трудяга с этой чертой не заболевает независимо от шанса болезни рецепта.
    /// </summary>
    public bool NoSick { get; set; }

    /// <summary>
    /// Бонус в процентах к весу редкой находки в экспедиции для отряда, где есть трудяга с этой чертой.
    /// </summary>
    /// <value>Проценты.</value>
    /// <remarks>
    /// См. <see cref="Activities.ExpeditionManager.ScaleWeight"/> – берётся максимум по отряду.
    /// </remarks>
    public int LuckWeightPercent { get; set; }
}
