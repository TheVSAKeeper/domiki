namespace Domiki.Web.Models
{
    public class WorkerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TraitId { get; set; }
        public string TraitName { get; set; }
        public int TraitDurationPercent { get; set; }
        public bool NoFatigue { get; set; }
        public int? ManufactureId { get; set; }
        public int? ExpeditionId { get; set; }
        public DateTime? RestUntil { get; set; }
        public WorkerSkillDto[] Skills { get; set; }
    }
}
