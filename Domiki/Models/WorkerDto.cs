namespace Domiki.Web.Models
{
    public class WorkerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TraitId { get; set; }
        public string TraitName { get; set; }
        public int TraitDurationPercent { get; set; }
        public int? ManufactureId { get; set; }
    }
}
