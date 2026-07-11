namespace Domiki.Web.Models
{
    public class DecorStateDto
    {
        public DecorTypeDto[] Types { get; set; }
        public PlayerDecorDto[] Owned { get; set; }
        public int Comfort { get; set; }
    }

    public class DecorTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int ComfortPoints { get; set; }
        public bool IsPurchasable { get; set; }
        public ResourceDto[] Cost { get; set; }
    }

    public class PlayerDecorDto
    {
        public int DecorTypeId { get; set; }
        public int Count { get; set; }
    }
}
