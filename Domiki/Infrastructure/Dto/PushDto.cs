
namespace Domiki.Web.Infrastructure.Dto
{
    public class PushSubscribeDto
    {
        public string Endpoint { get; set; }
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }

    public class PushUnsubscribeDto
    {
        public string Endpoint { get; set; }
    }
}
