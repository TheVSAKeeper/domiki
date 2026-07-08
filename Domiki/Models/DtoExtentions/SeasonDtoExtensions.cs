using Domiki.Web.Business.Models;

namespace Domiki.Web.Models
{
    public static class SeasonDtoExtensions
    {
        public static SeasonDto ToDto(this Season season)
        {
            return new SeasonDto
            {
                Number = season.Number + 1,
                StartDate = season.StartDate,
                EndDate = season.EndDate,
            };
        }
    }
}
