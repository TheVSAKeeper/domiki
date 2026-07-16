
namespace Domiki.Web.Economy.Models
{
    public class TradeLot
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

    public class MarketState
    {
        public TradeLot[] Lots { get; set; }
        public TradeLot[] MyLots { get; set; }
        public int BuildingLevel { get; set; }
        public double CommissionRate { get; set; }
        public int CommissionMin { get; set; }
        public double? NextCommissionRate { get; set; }
        public int MaxLots { get; set; }
    }
}
