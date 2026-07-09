namespace Domiki.Web.Models
{
    public class GameStateDto
    {
        public DomikTypeDto[] DomikTypes { get; set; }
        public ResourceTypeDto[] ResourceTypes { get; set; }
        public ReceiptDto[] Receipts { get; set; }
        public DomikDto[] Domiks { get; set; }
        public ResourceDto[] Resources { get; set; }
        public OrderDto[] Orders { get; set; }
        public NeighborReputationDto[] Reputation { get; set; }
        public BlueprintDto[] Blueprints { get; set; }
        public VillageDto Village { get; set; }
        public VillageLevelDto VillageLevel { get; set; }
        public WorkerDto[] Workers { get; set; }
        public DomikTypeDto[] PurchaseAvailableDomiks { get; set; }
        public WeatherStateDto Weather { get; set; }
        public ExpeditionStateDto Expeditions { get; set; }
        public DecorStateDto Decor { get; set; }
        public TolokaStateDto Toloka { get; set; }
        public MarketStateDto Market { get; set; }
        public RecapDto Recap { get; set; }
    }
}
