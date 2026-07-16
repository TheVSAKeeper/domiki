namespace Domiki.Web.Core.Dto;

public class DomikTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LogicName { get; set; }
    public int MaxCount { get; internal set; }
    public int AvailableCount { get; internal set; }
    public int MaxLevel { get; internal set; }
    public int UnlockLevel { get; internal set; }
    public int? BlueprintId { get; internal set; }
    public int? NextCountGateLevel { get; internal set; }

    public UpgradeLevelDto[] Levels { get; set; }
}
