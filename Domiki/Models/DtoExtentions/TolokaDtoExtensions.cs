using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class TolokaDtoExtensions
    {
        public static TolokaStateDto ToDto(this TolokaState state)
        {
            return new TolokaStateDto
            {
                Active = state.Active.ToDto(),
                MyContribution = state.MyContribution,
                CanContribute = state.CanContribute,
                UnlockLevel = state.UnlockLevel,
                BuffActive = state.BuffActive,
                BuffUntil = state.BuffUntil == null ? null : DateTime.SpecifyKind(state.BuffUntil.Value, DateTimeKind.Utc),
                BuffPercent = state.BuffPercent,
            };
        }

        public static TolokaDto ToDto(this Toloka toloka)
        {
            return new TolokaDto
            {
                Id = toloka.Id,
                TolokaTypeId = toloka.TolokaType.Id,
                Name = toloka.TolokaType.Name,
                LogicName = toloka.TolokaType.LogicName,
                ResourceTypeId = toloka.TolokaType.ResourceTypeId,
                Goal = toloka.TolokaType.Goal,
                Collected = toloka.Collected,
                StartDate = DateTime.SpecifyKind(toloka.StartDate, DateTimeKind.Utc),
            };
        }
    }
}
