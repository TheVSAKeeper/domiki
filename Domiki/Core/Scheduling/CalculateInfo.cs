using Domiki.Web.Data.Entities;
using System.Timers;

namespace Domiki.Web.Core.Scheduling
{
    public class CalculateInfo
    {
        public int PlayerId { get; set; }
        public int ObjectId { get; set; }

        /// <summary>
        /// дата когда событие должно выполнится
        /// </summary>
        public DateTime Date { get; set; }
        public CalculateTypes Type { get; set; }

        public string PushTitle { get; set; }
        public string PushBody { get; set; }
    }
}
