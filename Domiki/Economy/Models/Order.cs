using Domiki.Web.Reference.Models;

namespace Domiki.Web.Economy.Models;

public class Order
{
    public int Id { get; set; }
    public required Neighbor Neighbor { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ExpireDate { get; set; }
    public Resource[] Resources { get; set; } = [];
    public int RewardCoins { get; set; }
    public int RewardGold { get; set; }
    public int RewardReputation { get; set; }
}
