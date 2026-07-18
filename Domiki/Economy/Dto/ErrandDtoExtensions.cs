using Domiki.Web.Economy.Models;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Economy.Dto;

public static class ErrandDtoExtensions
{
    public static ErrandDto ToDto(this Errand errand)
    {
        return new()
        {
            Id = errand.Id,
            NeighborId = errand.Neighbor.Id,
            NeighborName = errand.Neighbor.Name,
            NeighborLogicName = errand.Neighbor.LogicName,
            TemplateId = errand.TemplateId,
            ExpireDate = DateTimeHelper.AsUtc(errand.ExpireDate),
            AcceptDate = DateTimeHelper.AsUtc(errand.AcceptDate),
            ClueId = errand.ClueId,
            FinishDate = DateTimeHelper.AsUtc(errand.FinishDate),
            WorkerIds = errand.WorkerIds,
        };
    }
}
