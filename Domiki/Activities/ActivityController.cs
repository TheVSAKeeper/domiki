using Domiki.Web.Activities.Dto;
using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Activities;

public class ActivityController : GameControllerBase
{
    private readonly BlueprintManager _blueprintManager;
    private readonly TolokaManager _tolokaManager;
    private readonly ExpeditionManager _expeditionManager;

    public ActivityController(DomikManager domikManager, BlueprintManager blueprintManager, TolokaManager tolokaManager, ExpeditionManager expeditionManager)
        : base(domikManager)
    {
        _blueprintManager = blueprintManager;
        _tolokaManager = tolokaManager;
        _expeditionManager = expeditionManager;
    }

    [HttpGet]
    [Route("/Domiki/GetBlueprints")]
    public Response<BlueprintDto[]> GetBlueprints()
    {
        var playerId = GetPlayerId();

        var content = _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetToloka")]
    public Response<TolokaStateDto> GetToloka()
    {
        var playerId = GetPlayerId();

        var content = _tolokaManager.GetToloka(DateTimeHelper.GetNowDate(), playerId)?.ToDto();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/ContributeToloka/{amount}")]
    public Response ContributeToloka(int amount)
    {
        var playerId = GetPlayerId();
        _tolokaManager.Contribute(playerId, amount, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }

    [HttpGet]
    [Route("/Domiki/GetExpeditions")]
    public Response<ExpeditionStateDto> GetExpeditions()
    {
        var playerId = GetPlayerId();

        var content = _expeditionManager.GetExpeditions(playerId)?.ToDto();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/StartExpedition/{expeditionTypeId}")]
    public Response StartExpedition(int expeditionTypeId, [FromQuery] int[] workerIds = null, [FromQuery] bool provisions = false)
    {
        var playerId = GetPlayerId();
        _expeditionManager.StartExpedition(playerId, expeditionTypeId, workerIds, provisions);
        return new()
            { Type = ResponseType.Success };
    }
}
