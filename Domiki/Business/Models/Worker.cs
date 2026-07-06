namespace Domiki.Web.Business.Models
{
    public class Worker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Trait Trait { get; set; }
        public int? ManufactureId { get; set; }
        public int WorkedSeconds { get; set; }
        public DateTime? RestUntil { get; set; }
        public WorkerSkill[] Skills { get; set; }
    }
}
