using Domiki.Web.Core.Dto;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Core;

public class DomikController : GameControllerBase
{
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;

    public DomikController(DomikManager domikManager, ResourceManager resourceManager)
        : base(domikManager)
    {
        _domikManager = domikManager;
        _resourceManager = resourceManager;
    }

    [HttpGet]
    [Route("/Domiki/GetDomiks")]
    public Response<DomikDto[]> GetDomiks()
    {
        var playerId = GetPlayerId();

        var content = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetResources")]
    public Response<ResourceDto[]> GetResources()
    {
        var playerId = GetPlayerId();

        var content = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetPurchaseAvaialableDomiks")]
    public Response<DomikTypeDto[]> GetPurchaseAvaialableDomiks()
    {
        var playerId = GetPlayerId();
        var blueprints = _resourceManager.GetBlueprints();
        var content = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id, x.NextCountGateLevel)).ToArray();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/BuyDomik/{typeId}")]
    public Response BuyDomik(int typeId)
    {
        var playerId = GetPlayerId();
        _domikManager.BuyDomik(playerId, typeId);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/UpgradeDomik/{id}")]
    public Response UpgradeDomik(int id)
    {
        var playerId = GetPlayerId();
        _domikManager.UpgradeDomik(playerId, id);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/HurryDomik/{id}")]
    public Response HurryDomik(int id)
    {
        var playerId = GetPlayerId();
        _domikManager.HurryDomik(playerId, id);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/StartManufacture/{domikId}/{receiptId}")]
    public Response StartManufacture(int domikId, int receiptId, [FromQuery] bool useOptional = false, [FromQuery] int[] workerIds = null, [FromQuery] bool autoRepeat = false)
    {
        var playerId = GetPlayerId();
        _domikManager.StartManufacture(playerId, domikId, receiptId, useOptional, workerIds, autoRepeat);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/HurryManufacture/{manufactureId}")]
    public Response HurryManufacture(int manufactureId)
    {
        var playerId = GetPlayerId();
        _domikManager.HurryManufacture(playerId, manufactureId);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/SetManufactureAutoRepeat/{manufactureId}")]
    public Response SetManufactureAutoRepeat(int manufactureId, [FromQuery] bool autoRepeat)
    {
        var playerId = GetPlayerId();
        _domikManager.SetManufactureAutoRepeat(playerId, manufactureId, autoRepeat);
        return new()
            { Type = ResponseType.Success };
    }
}
