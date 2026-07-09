using Domiki.Web.Data;
using System.Text.Json;

namespace Domiki.Web.Business.Models
{
    public class RecapModel
    {
        public int AwaySeconds { get; set; }
        public List<RecapEventModel> Events { get; set; }
    }

    public class RecapEventModel
    {
        public PlayerEventType Type { get; set; }
        public DateTime Date { get; set; }
        public JsonElement Data { get; set; }
    }
}
