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
    public BlueprintDto[] GetBlueprints()
    {
        var playerId = GetPlayerId();

        return _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetToloka")]
    public TolokaStateDto? GetToloka()
    {
        var playerId = GetPlayerId();

        return _tolokaManager.GetToloka(DateTimeHelper.GetNowDate(), playerId)?.ToDto();
    }

    [HttpPost]
    [Route("/Domiki/ContributeToloka/{resourceTypeId}/{amount}")]
    public void ContributeToloka(int resourceTypeId, int amount)
    {
        var playerId = GetPlayerId();
        _tolokaManager.Contribute(playerId, resourceTypeId, amount, DateTimeHelper.GetNowDate());
    }

    [HttpGet]
    [Route("/Domiki/GetExpeditions")]
    public ExpeditionStateDto? GetExpeditions()
    {
        var playerId = GetPlayerId();

        return _expeditionManager.GetExpeditions(playerId)?.ToDto();
    }

    [HttpPost]
    [Route("/Domiki/StartExpedition/{expeditionTypeId}")]
    public void StartExpedition(int expeditionTypeId, [FromQuery] int[]? workerIds = null, [FromQuery] bool provisions = false)
    {
        var playerId = GetPlayerId();
        _expeditionManager.StartExpedition(playerId, expeditionTypeId, workerIds, provisions);
    }
}
