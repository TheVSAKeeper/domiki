using Domiki.Web.Reference.Models;

namespace Domiki.Web.Reference.Dto;

public static class ReceiptDtoExtensions
{
    public static ReceiptDto ToDto(this Receipt res)
    {
        return new()
        {
            Id = res.Id,
            Name = res.Name ?? "",
            LogicName = res.LogicName ?? "",
            InputResources = res.InputResources.Select(x => x.ToDto()).ToArray(),
            OptionalInputResources = res.OptionalInputResources.Select(x => x.ToDto()).ToArray(),
            DurationSeconds = res.DurationSeconds,
            OutputBonusPercent = res.OutputBonusPercent,
            OutputResources = res.OutputResources.Select(x => x.ToDto()).ToArray(),
            PlodderCount = res.PlodderCount,
        };
    }
}
