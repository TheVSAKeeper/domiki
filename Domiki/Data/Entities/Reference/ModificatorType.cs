using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("ModificatorTypes")]
public class ModificatorType
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string LogicName { get; set; }
}
