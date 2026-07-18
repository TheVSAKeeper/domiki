using Domiki.Web.Infrastructure.Models;

namespace Domiki.Web.Infrastructure.Dto;

public static class RecapDtoExtensions
{
    public static RecapDto ToDto(this RecapModel model)
    {
        return new()
        {
            AwaySeconds = model.AwaySeconds,
            Events = model.Events.Select(x => x.ToDto()).ToArray(),
        };
    }

    public static RecapEventDto ToDto(this RecapEventModel model)
    {
        return new()
        {
            Type = model.Type.ToString(),
            Date = DateTimeHelper.AsUtc(model.Date),
            Data = model.Data,
        };
    }
}
