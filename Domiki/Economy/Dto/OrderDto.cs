
namespace Domiki.Web.Economy.Dto
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int NeighborId { get; set; }
        public string NeighborName { get; set; }
        public string NeighborLogicName { get; set; }
        public DateTime ExpireDate { get; set; }
        public OrderResourceDto[] Required { get; set; }
        public int RewardCoins { get; set; }
        public int RewardGold { get; set; }
        public int RewardReputation { get; set; }
    }

    public class OrderResourceDto
    {
        public int ResourceTypeId { get; set; }
        public int Value { get; set; }
    }
}
