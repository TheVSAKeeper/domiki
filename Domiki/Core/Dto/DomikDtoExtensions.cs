using Domiki.Web.Core.Models;
using Domiki.Web.Infrastructure;

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
            FinishDate = DateTimeHelper.AsUtc(domik.FinishDate),
            UpgradeSeconds = domik.UpgradeSeconds,
            Manufactures = domik.Manufactures.Select(x => new ManufactureDto
                {
                    Id = x.Id,
                    FinishDate = DateTimeHelper.AsUtc(x.FinishDate),
                    DurationSeconds = x.DurationSeconds,
                    PlodderCount = x.PlodderCount,
                    ReceiptId = x.ReceiptId,
                    AutoRepeat = x.AutoRepeat,
                })
                .ToArray(),
        };
    }
}
