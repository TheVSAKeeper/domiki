using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

[Table("Receipts")]
public class Receipt
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public string LogicName { get; set; }

    public int DurationSeconds { get; set; }

    public int OutputBonusPercent { get; set; }

    public int PlodderCount { get; set; }

    public ICollection<ReceiptResource> Resources { get; set; }
}
