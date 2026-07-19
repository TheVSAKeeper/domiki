using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Activities.Dto;

public static class IncidentDtoExtensions
{
    public static IncidentDto ToDto(this Incident incident)
    {
        return new()
        {
            Id = incident.Id,
            MissingWorkerId = incident.MissingWorkerId,
            ExpeditionTypeId = incident.ExpeditionTypeId,
            TemplateId = incident.TemplateId,
            CreateDate = DateTimeHelper.AsUtc(incident.CreateDate),
            ClueId = incident.ClueId,
            SearchEndDate = DateTimeHelper.AsUtc(incident.SearchEndDate),
            AutoReturnDate = DateTimeHelper.AsUtc(incident.AutoReturnDate),
            SearchWorkerIds = incident.SearchWorkerIds,
        };
    }
}
