using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

public static class SeasonDtoExtensions
{
    public static SeasonDto ToDto(this Season season)
    {
        return new()
        {
            Number = season.Number + 1,
            StartDate = season.StartDate,
            EndDate = season.EndDate,
        };
    }
}
