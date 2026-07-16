namespace Domiki.Web.Core.Models;

public class Manufacture
{
    public int Id { get; set; }
    public DateTime FinishDate { get; set; }
    public int PlodderCount { get; set; }
    public int ReceiptId { get; set; }
    public bool AutoRepeat { get; set; }
}
