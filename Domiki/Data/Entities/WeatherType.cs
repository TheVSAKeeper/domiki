using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data
{
    [Table("WeatherTypes")]
    public class WeatherType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string LogicName { get; set; }

        public int RotationWeight { get; set; }
    }
}
