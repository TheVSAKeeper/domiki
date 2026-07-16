namespace Domiki.Web.Data.Entities
{
    public class PlayerGoal
    {
        public int PlayerId { get; set; }

        public int GoalId { get; set; }

        public DateTime CompleteDate { get; set; }
    }
}
