namespace Domiki.Web.Data.Entities;

/// <summary>
/// Вид добычи, которую может выдать строка лут-таблицы похода (ExpeditionLoot.Kind).
/// </summary>
public enum ExpeditionLootKind
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Случайное количество ресурса (<see cref="ExpeditionLoot.MinValue"/>..<see cref="ExpeditionLoot.MaxValue"/>).
    /// </summary>
    Resource = 1,

    /// <summary>
    /// Единица декора.
    /// </summary>
    Decor = 2,

    /// <summary>
    /// Один из обычных трудяг отряда получает случайную не-обычную черту.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Activities.ExpeditionManager.ApplyTraitUpgrade"/>.
    /// </remarks>
    TraitUpgrade = 3,

    /// <summary>
    /// Чертёж постройки – конкретный (<see cref="ExpeditionLoot.BlueprintId"/>) или случайный из ещё не полученных игроком.
    /// </summary>
    Blueprint = 4,
}
