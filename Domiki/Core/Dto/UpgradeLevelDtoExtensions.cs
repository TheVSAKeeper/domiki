using Domiki.Web.Core.Models;
using Domiki.Web.Reference.Dto;

namespace Domiki.Web.Core.Dto;

public static class UpgradeLevelDtoExtensions
{
    public static UpgradeLevelDto ToDto(this UpgradeLevel t)
    {
        return new()
        {
            Value = t.Value,
            Resources = t.Resources.Select(x => x.ToDto()).ToArray(),
            Modificators = t.Modificators.Select(x => x.ToDto()).ToArray(),
            ReceiptIds = t.Receipts.Select(x => x.Id).ToArray(),
            MaxManufactureCount = t.MaxManufactureCount,
        };
    }
}
