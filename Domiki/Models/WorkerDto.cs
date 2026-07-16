namespace Domiki.Web.Models
{
    public class WorkerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }
        public int TraitId { get; set; }
        public string TraitName { get; set; }
        public string TraitLogicName { get; set; }
        public int TraitDurationPercent { get; set; }
        public bool NoFatigue { get; set; }
        public bool NoSick { get; set; }
        public int? ManufactureId { get; set; }
        public int? ExpeditionId { get; set; }
        public DateTime? RestUntil { get; set; }
        public DateTime? SickUntil { get; set; }
        public WorkerSkillDto[] Skills { get; set; }
    }
}
