
namespace Domiki.Web.Activities.Models
{
    public class Expedition
    {
        public int Id { get; set; }
        public ExpeditionType ExpeditionType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime FinishDate { get; set; }
    }
}
