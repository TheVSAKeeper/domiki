using Domiki.Web.Infrastructure;
using Domiki.Web.Workers.Models;

namespace Domiki.Web.Workers.Dto;

public static class WorkerDtoExtensions
{
    public static WorkerDto ToDto(this Worker worker)
    {
        var now = DateTimeHelper.GetNowDate();

        return new()
        {
            Id = worker.Id,
            Name = worker.Name,
            Gender = (int)NameGrammar.GenderOf(worker.Name),
            TraitId = worker.Trait.Id,
            TraitName = worker.Trait.Name,
            TraitLogicName = worker.Trait.LogicName,
            TraitDurationPercent = worker.Trait.DurationPercent,
            NoFatigue = worker.Trait.NoFatigue,
            NoSick = worker.Trait.NoSick,
            ManufactureId = worker.ManufactureId,
            ExpeditionId = worker.ExpeditionId,
            ErrandId = worker.ErrandId,
            IncidentId = worker.IncidentId,
            WorkedSeconds = worker.WorkedSeconds,
            RestUntil = worker.RestUntil > now ? DateTimeHelper.AsUtc(worker.RestUntil) : null,
            SickUntil = worker.SickUntil > now ? DateTimeHelper.AsUtc(worker.SickUntil) : null,
            SickTypeId = worker.SickTypeId,
            IsAway = worker.IsAway,
            Skills = worker.Skills.Select(x => new WorkerSkillDto
                {
                    DomikTypeId = x.DomikTypeId,
                    Uses = x.Uses,
                    BonusPercent = x.BonusPercent,
                })
                .ToArray(),
        };
    }
}
