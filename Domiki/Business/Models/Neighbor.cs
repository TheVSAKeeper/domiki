namespace Domiki.Web.Business.Models
{
    public class Neighbor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int PrimaryResourceTypeId { get; set; }
        public int UnlockLevel { get; set; }
    }
}
