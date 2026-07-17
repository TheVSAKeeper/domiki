using Domiki.Web.Core.Models;

namespace Domiki.Web.Core.Dto;

public static class DomikDtoExtensions
{
    public static DomikDto ToDto(this Domik domik)
    {
        return new()
        {
            Id = domik.Id,
            Level = domik.Level,
            TypeId = domik.Type.Id,
            // todo вынести в нормальный хэлпер или в БД с часовым поясом хранить, или ещё чего
            FinishDate = domik.FinishDate == null ? null : DateTime.SpecifyKind(domik.FinishDate.Value, DateTimeKind.Utc),
            UpgradeSeconds = domik.UpgradeSeconds,
            Manufactures = domik.Manufactures.Select(x => new ManufactureDto
                {
                    Id = x.Id,
                    FinishDate = DateTime.SpecifyKind(x.FinishDate, DateTimeKind.Utc),
                    PlodderCount = x.PlodderCount,
                    ReceiptId = x.ReceiptId,
                    AutoRepeat = x.AutoRepeat,
                })
                .ToArray(),
        };
    }
}
