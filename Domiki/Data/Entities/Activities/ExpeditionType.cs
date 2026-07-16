using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data.Entities
{
    [Table("ExpeditionTypes")]
    public class ExpeditionType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string LogicName { get; set; }

        public int DurationSeconds { get; set; }

        public int WorkerCount { get; set; }

        public int GoldCost { get; set; }

        public int RollCount { get; set; }
    }
}
