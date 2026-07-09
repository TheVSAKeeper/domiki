namespace Domiki.Web.Models
{
    public class RecapDto
    {
        public int AwaySeconds { get; set; }
        public RecapEventDto[] Events { get; set; }
    }

    public class RecapEventDto
    {
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public object Data { get; set; }
    }
}
