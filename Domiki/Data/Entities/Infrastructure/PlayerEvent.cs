using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data
{
    public class PlayerEvent
    {
        [Key]
        public long Id { get; set; }

        public int PlayerId { get; set; }

        public PlayerEventType Type { get; set; }

        public DateTime Date { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Data { get; set; }

        public bool Read { get; set; }
    }
}
