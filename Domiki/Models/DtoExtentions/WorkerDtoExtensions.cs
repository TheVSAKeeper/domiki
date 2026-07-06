using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class WorkerDtoExtensions
    {
        public static WorkerDto ToDto(this Worker worker)
        {
            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                TraitId = worker.Trait.Id,
                TraitName = worker.Trait.Name,
                TraitDurationPercent = worker.Trait.DurationPercent,
                ManufactureId = worker.ManufactureId,
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
