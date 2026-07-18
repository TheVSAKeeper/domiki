using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Activities.Dto;

public static class TolokaDtoExtensions
{
    public static TolokaStateDto ToDto(this TolokaState state)
    {
        return new()
        {
            Active = state.Active.ToDto(),
            MyContribution = state.MyContribution,
            ActiveBuffs = state.ActiveBuffs.Select(b => new TolokaActiveBuffDto
                {
                    LogicName = b.LogicName,
                    Label = b.Label,
                    Percent = b.Percent,
                    BuffUntil = DateTimeHelper.AsUtc(b.BuffUntil),
                })
                .ToArray(),
            BuffHours = state.BuffHours,
            NextBuffHours = state.NextBuffHours,
        };
    }

    public static TolokaDto ToDto(this Toloka toloka)
    {
        return new()
        {
            Id = toloka.Id,
            TolokaTypeId = toloka.TolokaType.Id,
            Name = toloka.TolokaType.Name,
            LogicName = toloka.TolokaType.LogicName,
            ResourceTypeId = toloka.TolokaType.ResourceTypeId,
            Goal = toloka.Goal,
            Collected = toloka.Collected,
            StartDate = DateTimeHelper.AsUtc(toloka.StartDate),
        };
    }

    public static TolokaArtifactDto ToDto(this TolokaArtifact artifact)
    {
        return new()
        {
            Name = artifact.Name,
            ResourceName = artifact.ResourceName,
            Goal = artifact.Goal,
            SeasonNumber = artifact.SeasonNumber + 1,
            Participants = artifact.Participants,
            CompletedDate = DateTimeHelper.AsUtc(artifact.CompletedDate),
        };
    }
}
