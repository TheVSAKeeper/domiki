using Domiki.Web.Data.Entities;

namespace Domiki.Web.Activities.Models;

public class TolokaType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string LogicName { get; set; }
    public int ResourceTypeId { get; set; }
    public int Goal { get; set; }
    public int RotationWeight { get; set; }
    public TolokaTypeEffect[] Effects { get; set; } = [];
}
