
namespace Domiki.Web.Activities.Dto
{
    public class GoalsStateDto
    {
        public ActiveGoalDto Active { get; set; }
        public int CompletedCount { get; set; }
        public int TotalCount { get; set; }
        public int ZealCharges { get; set; }
    }

    public class ActiveGoalDto
    {
        public int Id { get; set; }
        public int Ordinal { get; set; }
        public string Name { get; set; }
        public int RewardCoins { get; set; }
    }
}
