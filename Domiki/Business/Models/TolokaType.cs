namespace Domiki.Web.Business.Models
{
    public class TolokaType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int ResourceTypeId { get; set; }
        public int Goal { get; set; }
        public int RotationWeight { get; set; }
    }
}
