using Domiki.Web.Data;

namespace Domiki.Web.Business.Models
{
    public class ExpeditionType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int DurationSeconds { get; set; }
        public int WorkerCount { get; set; }
        public int GoldCost { get; set; }
        public int RollCount { get; set; }
        public ExpeditionLoot[] Loot { get; set; }
        public ExpeditionEquipment[] Equipment { get; set; }
    }

    public class ExpeditionEquipment
    {
        public int ResourceTypeId { get; set; }
        public int Value { get; set; }
    }

    public class ExpeditionLoot
    {
        public ExpeditionLootKind Kind { get; set; }
        public int? ResourceTypeId { get; set; }
        public int? DecorTypeId { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Weight { get; set; }
        public bool IsRare { get; set; }
    }
}
