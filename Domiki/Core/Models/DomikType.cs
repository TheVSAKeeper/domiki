namespace Domiki.Web.Core.Models;

public class DomikType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string LogicName { get; set; }

    /// <summary>
    /// Максимальное количество построек данного типа.
    /// </summary>
    public int MaxCount { get; set; }

    public int UnlockLevel { get; set; }

    /// <summary>
    /// Максимальный уровень, до которого можно улучший.
    /// </summary>
    public int MaxLevel => Levels.Length;

    public UpgradeLevel[] Levels { get; set; } = [];
}
