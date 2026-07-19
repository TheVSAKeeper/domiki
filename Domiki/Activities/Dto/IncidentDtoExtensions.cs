using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Activities.Dto;

public static class IncidentDtoExtensions
{
    /// <summary>
    /// Преобразует происшествие с пропавшим в походе трудягой в DTO.
    /// </summary>
    /// <param name="incident">Активное происшествие.</param>
    /// <returns>DTO для передачи на клиент.</returns>
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

    /// <summary>
    /// Преобразует происшествие-загадку в постройке в DTO.
    /// </summary>
    /// <param name="incident">Активное происшествие.</param>
    /// <returns>DTO для передачи на клиент.</returns>
    public static DomikIncidentDto ToDto(this DomikIncident incident)
    {
        return new()
        {
            Id = incident.Id,
            DomikTypeId = incident.DomikTypeId,
            TemplateId = incident.TemplateId,
            CreateDate = DateTimeHelper.AsUtc(incident.CreateDate),
            ClueId = incident.ClueId,
            SearchEndDate = DateTimeHelper.AsUtc(incident.SearchEndDate),
            AutoResolveDate = DateTimeHelper.AsUtc(incident.AutoResolveDate),
            SearchWorkerIds = incident.SearchWorkerIds,
        };
    }
}
