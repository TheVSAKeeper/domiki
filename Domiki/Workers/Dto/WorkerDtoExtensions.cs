using Domiki.Web.Workers.Models;

namespace Domiki.Web.Workers.Dto
{
    public static class WorkerDtoExtensions
    {
        public static WorkerDto ToDto(this Worker worker)
        {
            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                Gender = (int)Domiki.Web.Workers.NameGrammar.GenderOf(worker.Name),
                TraitId = worker.Trait.Id,
                TraitName = worker.Trait.Name,
                TraitLogicName = worker.Trait.LogicName,
                TraitDurationPercent = worker.Trait.DurationPercent,
                NoFatigue = worker.Trait.NoFatigue,
                NoSick = worker.Trait.NoSick,
                ManufactureId = worker.ManufactureId,
                ExpeditionId = worker.ExpeditionId,
                RestUntil = worker.RestUntil,
                SickUntil = worker.SickUntil,
                Skills = worker.Skills.Select(x => new WorkerSkillDto
                {
                    DomikTypeId = x.DomikTypeId,
                    Uses = x.Uses,
                    BonusPercent = x.BonusPercent,
                }).ToArray(),
            };
        }
    }
}
