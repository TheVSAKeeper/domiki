namespace Domiki.Web.Workers.Models;

/// <summary>
/// Справочник черт трудяг – каждому новому трудяге случайно назначается одна черта, задающая его особенности в производстве, отдыхе, здоровье и удаче.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Reference.ResourceManager.GetTraits"/>.
/// </remarks>
public class Trait
{
    /// <summary>
    /// Идентификатор черты.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название черты.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код черты, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Изменение длительности производства в процентах.
    /// </summary>
    /// <value>Проценты; отрицательное значение – трудяга работает быстрее обычного, положительное – медленнее.</value>
    public int DurationPercent { get; set; }

    /// <summary>
    /// Трудяга с этой чертой не устаёт и не уходит на отдых.
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
