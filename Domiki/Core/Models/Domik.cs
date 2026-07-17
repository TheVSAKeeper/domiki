namespace Domiki.Web.Core.Models;

public class Domik
{
    public int Id { get; set; }
    public required DomikType Type { get; set; }
    public int Level { get; set; }
    public DateTime? FinishDate { get; set; }
    public int? UpgradeSeconds { get; set; }
    public Manufacture[] Manufactures { get; set; } = [];
}
