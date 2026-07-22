using Domiki.Web.Infrastructure;
using Domiki.Web.Workers.Models;

namespace Domiki.Web.Workers.Dto;

public static class WorkerDtoExtensions
{
    public static WorkerDto ToDto(this Worker worker)
    {
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
            RestUntil = DateTimeHelper.AsUtc(worker.RestUntil),
            SickUntil = DateTimeHelper.AsUtc(worker.SickUntil),
            SickTypeId = worker.SickTypeId,
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
