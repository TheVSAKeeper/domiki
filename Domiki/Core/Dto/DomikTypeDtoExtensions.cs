using Domiki.Web.Core.Models;

namespace Domiki.Web.Core.Dto
{
    public static class DomikTypeDtoExtensions
    {
        public static DomikTypeDto ToDto(this DomikType t, int availableCount = 0, int? blueprintId = null, int? nextCountGateLevel = null)
        {
            return new DomikTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                LogicName = t.LogicName,
                MaxCount = t.MaxCount,
                AvailableCount = availableCount,
                MaxLevel = t.MaxLevel,
                UnlockLevel = t.UnlockLevel,
                BlueprintId = blueprintId,
                NextCountGateLevel = nextCountGateLevel,
                Levels = t.Levels.Select(x => x.ToDto()).ToArray(),
            };
        }
    }
}
