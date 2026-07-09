using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class RecapDtoExtensions
    {
        public static RecapDto ToDto(this RecapModel model)
        {
            return new RecapDto
            {
                AwaySeconds = model.AwaySeconds,
                Events = model.Events.Select(x => x.ToDto()).ToArray(),
            };
        }

        public static RecapEventDto ToDto(this RecapEventModel model)
        {
            return new RecapEventDto
            {
                Type = model.Type.ToString(),
                Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc),
                Data = model.Data,
            };
        }
    }
}
