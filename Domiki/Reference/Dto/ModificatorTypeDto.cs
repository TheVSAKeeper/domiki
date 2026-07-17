namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Справочник типа модификатора – какой именно эффект даёт уровень постройки.
/// </summary>
public sealed record ModificatorTypeDto
{
    /// <summary>
    /// Идентификатор типа модификатора.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; init; }
}
