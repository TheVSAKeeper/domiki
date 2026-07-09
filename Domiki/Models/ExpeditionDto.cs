namespace Domiki.Web.Models
{
    public class ExpeditionStateDto
    {
        public ExpeditionDto[] Active { get; set; }
        public ExpeditionTypeDto[] Types { get; set; }
        public int ExpeditionsSincePity { get; set; }
        public int PityThreshold { get; set; }
        public int MaxActive { get; set; }
    }

    public class ExpeditionDto
    {
        public int Id { get; set; }
        public int ExpeditionTypeId { get; set; }
        public string ExpeditionName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime FinishDate { get; set; }
    }

    public class ExpeditionTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int DurationSeconds { get; set; }
        public int WorkerCount { get; set; }
        public int GoldCost { get; set; }
        public int RollCount { get; set; }
        public ExpeditionLootDto[] Loot { get; set; }
        public ExpeditionEquipmentDto[] Equipment { get; set; }
    }

    public class ExpeditionEquipmentDto
    {
        public int ResourceTypeId { get; set; }
        public int Value { get; set; }
    }

    public class ExpeditionLootDto
    {
        public int ResourceTypeId { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool IsRare { get; set; }
    }
}
