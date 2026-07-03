using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class DomikTypeDtoExtensions
    {
        public static DomikTypeDto ToDto(this DomikType t, int availableCount = 0)
        {
            return new DomikTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                LogicName = t.LogicName,
                MaxCount = t.MaxCount,
                AvailableCount = availableCount,
                MaxLevel = t.MaxLevel,
                Levels = t.Levels.Select(x => x.ToDto()).ToArray(),
            };
        }
    }
}