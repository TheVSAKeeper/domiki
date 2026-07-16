using System.ComponentModel.DataAnnotations;

namespace Domiki.Web.Data
{
    public class PlayerPushSubscription
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Endpoint { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string P256dh { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Auth { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
