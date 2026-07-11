namespace Domiki.Web.Business.Models
{
    public class DecorType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogicName { get; set; }
        public int ComfortPoints { get; set; }
        public bool IsPurchasable { get; set; }
        public Resource[] Cost { get; set; }
    }

    public class PlayerDecor
    {
        public int DecorTypeId { get; set; }
        public int Count { get; set; }
    }

    public class DecorState
    {
        public DecorType[] Types { get; set; }
        public PlayerDecor[] Owned { get; set; }
        public int Comfort { get; set; }
    }
}
