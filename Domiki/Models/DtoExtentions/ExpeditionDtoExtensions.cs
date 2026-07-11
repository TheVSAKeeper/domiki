using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class ExpeditionDtoExtensions
    {
        public static ExpeditionStateDto ToDto(this ExpeditionState state)
        {
            return new ExpeditionStateDto
            {
                Active = state.Active.Select(x => x.ToDto()).ToArray(),
                Types = state.Types.Select(x => x.ToDto()).ToArray(),
                ExpeditionsSincePity = state.ExpeditionsSincePity,
                PityThreshold = state.PityThreshold,
                MaxActive = state.MaxActive,
            };
        }

        public static ExpeditionDto ToDto(this Expedition expedition)
        {
            return new ExpeditionDto
            {
                Id = expedition.Id,
                ExpeditionTypeId = expedition.ExpeditionType.Id,
                ExpeditionName = expedition.ExpeditionType.Name,
                StartDate = DateTime.SpecifyKind(expedition.StartDate, DateTimeKind.Utc),
                FinishDate = DateTime.SpecifyKind(expedition.FinishDate, DateTimeKind.Utc),
            };
        }

        public static ExpeditionTypeDto ToDto(this ExpeditionType type)
        {
            return new ExpeditionTypeDto
            {
                Id = type.Id,
                Name = type.Name,
                LogicName = type.LogicName,
                DurationSeconds = type.DurationSeconds,
                WorkerCount = type.WorkerCount,
                GoldCost = type.GoldCost,
                RollCount = type.RollCount,
                Loot = type.Loot.Select(x => new ExpeditionLootDto
                {
                    Kind = (int)x.Kind,
                    ResourceTypeId = x.ResourceTypeId,
                    DecorTypeId = x.DecorTypeId,
                    MinValue = x.MinValue,
                    MaxValue = x.MaxValue,
                    IsRare = x.IsRare,
                }).ToArray(),
                Equipment = type.Equipment.Select(x => new ExpeditionEquipmentDto
                {
                    ResourceTypeId = x.ResourceTypeId,
                    Value = x.Value,
                }).ToArray(),
            };
        }
    }
}
