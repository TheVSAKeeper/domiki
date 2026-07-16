using Domiki.Web.Activities.Models;

namespace Domiki.Web.Activities.Dto
{
    public static class GoalsDtoExtensions
    {
        public static GoalsStateDto ToDto(this GoalsState state)
        {
            return new GoalsStateDto
            {
                Active = state.ActiveGoal == null ? null : new ActiveGoalDto
                {
                    Id = state.ActiveGoal.Id,
                    Ordinal = state.ActiveGoal.Ordinal,
                    Name = state.ActiveGoal.Name,
                    RewardCoins = state.ActiveGoal.RewardCoins,
                },
                CompletedCount = state.CompletedCount,
                TotalCount = state.TotalCount,
                ZealCharges = state.ZealCharges,
            };
        }
    }
}
