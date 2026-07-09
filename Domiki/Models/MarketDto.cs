namespace Domiki.Web.Models
{
    public class TradeLotDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string SellerVillageName { get; set; }
        public int SellerCrestIcon { get; set; }
        public int SellerCrestColor { get; set; }
        public int GiveResourceTypeId { get; set; }
        public int GiveValue { get; set; }
        public int WantResourceTypeId { get; set; }
        public int WantValue { get; set; }
        public int CommissionCoins { get; set; }
        public DateTime ExpireDate { get; set; }
    }

    public class MarketStateDto
    {
        public TradeLotDto[] Lots { get; set; }
        public TradeLotDto[] MyLots { get; set; }
        public int Commission { get; set; }
    }
}
