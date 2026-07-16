using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("Blueprints")]
public class Blueprint
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string LogicName { get; set; }

    public int DomikTypeId { get; set; }

    public int NeighborId { get; set; }

    public int ReputationThreshold { get; set; }

    public DomikType DomikType { get; set; }

    public Neighbor Neighbor { get; set; }
}
