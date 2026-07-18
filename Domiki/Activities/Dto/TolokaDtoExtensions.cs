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
            Candidates = state.Candidates.Select(c => new TolokaVoteCandidateDto
                {
                    TolokaTypeId = c.TolokaTypeId,
                    Name = c.Name,
                    LogicName = c.LogicName,
                    Votes = c.Votes,
                })
                .ToArray(),
            MyVoteTolokaTypeId = state.MyVoteTolokaTypeId,
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
            Positions = toloka.Positions.Select(p => new TolokaPositionDto
                {
                    ResourceTypeId = p.ResourceTypeId,
                    Goal = p.Goal,
                    Collected = p.Collected,
                    MyContribution = p.MyContribution,
                })
                .ToArray(),
            StartDate = DateTimeHelper.AsUtc(toloka.StartDate),
        };
    }

    public static TolokaArtifactDto ToDto(this TolokaArtifact artifact)
    {
        return new()
        {
            Name = artifact.Name,
            ResourcesText = artifact.ResourcesText,
            SeasonNumber = artifact.SeasonNumber + 1,
            Participants = artifact.Participants,
            CompletedDate = DateTimeHelper.AsUtc(artifact.CompletedDate),
        };
    }
}
