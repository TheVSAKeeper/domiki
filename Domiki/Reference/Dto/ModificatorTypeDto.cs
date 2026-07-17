namespace Domiki.Web.Reference.Dto;

/// <summary>
/// Справочник типа модификатора – какой именно эффект даёт уровень постройки.
/// </summary>
public class ModificatorTypeDto
{
    /// <summary>
    /// Идентификатор типа модификатора.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public string LogicName { get; set; }
}
