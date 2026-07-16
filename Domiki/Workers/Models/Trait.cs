
namespace Domiki.Web.Workers.Models
{
    public class Trait
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int DurationPercent { get; set; }
        public bool NoFatigue { get; set; }
        public bool NoSick { get; set; }
        public int LuckWeightPercent { get; set; }
    }
}
