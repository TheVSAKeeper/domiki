
namespace Domiki.Web.Core.Dto
{
    public class ManufactureDto
    {
        public int Id { get; set; }
        public DateTime FinishDate { get; set; }
        public int PlodderCount { get; set; }
        public int ReceiptId { get; set; }
        public bool AutoRepeat { get; set; }
    }
}
