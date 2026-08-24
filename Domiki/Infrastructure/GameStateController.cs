using Domiki.Web.Activities;
using Domiki.Web.Activities.Dto;
using Domiki.Web.Core;
using Domiki.Web.Core.Dto;
using Domiki.Web.Economy;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Infrastructure.Dto;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Dto;
using Domiki.Web.Village;
using Domiki.Web.Village.Dto;
using Domiki.Web.Workers;
using Domiki.Web.Workers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Infrastructure;

public class GameStateController : GameControllerBase
{
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;
    private readonly OrderManager _orderManager;
    private readonly WorkerManager _workerManager;
    private readonly TavernManager _tavernManager;
    private readonly WeatherManager _weatherManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly BlueprintManager _blueprintManager;
    private readonly ExpeditionManager _expeditionManager;
    private readonly DecorManager _decorManager;
    private readonly TolokaManager _tolokaManager;
    private readonly MarketManager _marketManager;
    private readonly ConvoyManager _convoyManager;
    private readonly GiftManager _giftManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly GoalManager _goalManager;
    private readonly ErrandManager _errandManager;
    private readonly IncidentManager _incidentManager;
    private readonly WorkerMilestoneManager _workerMilestoneManager;
    private readonly ElderHouseManager _elderHouseManager;
    private readonly RelocationManager _relocationManager;

    public GameStateController(DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WorkerManager workerManager, TavernManager tavernManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, ExpeditionManager expeditionManager, DecorManager decorManager, TolokaManager tolokaManager, MarketManager marketManager, ConvoyManager convoyManager, GiftManager giftManager, PlayerEventManager playerEventManager, GoalManager goalManager, ErrandManager errandManager, IncidentManager incidentManager, WorkerMilestoneManager workerMilestoneManager, ElderHouseManager elderHouseManager, RelocationManager relocationManager)
        : base(domikManager)
    {
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _orderManager = orderManager;
        _workerManager = workerManager;
        _tavernManager = tavernManager;
        _weatherManager = weatherManager;
        _villageLevelCalculator = villageLevelCalculator;
        _blueprintManager = blueprintManager;
        _expeditionManager = expeditionManager;
        _decorManager = decorManager;
        _tolokaManager = tolokaManager;
        _marketManager = marketManager;
        _convoyManager = convoyManager;
        _giftManager = giftManager;
        _playerEventManager = playerEventManager;
        _goalManager = goalManager;
        _errandManager = errandManager;
        _incidentManager = incidentManager;
        _workerMilestoneManager = workerMilestoneManager;
        _elderHouseManager = elderHouseManager;
        _relocationManager = relocationManager;
    }

    [HttpGet]
    [Route("/Domiki/GetGameState")]
    public GameStateDto GetGameState()
    {
        var playerId = GetPlayerId();
        var goals = _goalManager.GetGoalsState(playerId);
        var blueprints = _resourceManager.GetBlueprints();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId);
        _giftManager.TryGrantGift(playerId, DateTimeHelper.GetNowDate());
        _workerMilestoneManager.TryGrantNext(playerId, villageLevel.Level, DateTimeHelper.GetNowDate());

        var toloka = _tolokaManager.GetToloka(DateTimeHelper.GetNowDate(), playerId);
        if (toloka != null)
        {
            toloka.Progress = _tolokaManager.TakeProgress(playerId);
        }

        var content = new GameStateDto
        {
            PlayerId = playerId,
            DomikTypes = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray(),
            ResourceTypes = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray(),
            Receipts = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray(),
            Domiks = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray(),
            Resources = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray(),
            Orders = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray(),
            Reputation = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray(),
            Errand = _errandManager.Get(playerId)?.ToDto(),
            Incident = _incidentManager.Get(playerId)?.ToDto(),
            DomikIncident = _incidentManager.GetDomik(playerId)?.ToDto(),
            Blueprints = _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray(),
            Village = _domikManager.GetVillage(playerId).ToDto(),
            VillageLevel = villageLevel.ToDto(),
            GoldMinedToday = _domikManager.GetGoldMinedToday(playerId),
            Workers = _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray(),
            Cloaks = _workerManager.GetCloakState(playerId).ToDto(),
            Larder = _tavernManager.GetRules(playerId).ToDto(),
            Ledger = _elderHouseManager.GetLedger(playerId)?.ToDto(),
            Reserves = _elderHouseManager.GetReserves(playerId).Select(x => x.ToDto()).ToArray(),
            SickTypes = _resourceManager.GetSickTypes().Select(x => x.ToDto()).ToArray(),
            PurchaseAvailableDomiks = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id, x.NextCountGateLevel)).ToArray(),
            Weather = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto(),
            Expeditions = _expeditionManager.GetExpeditions(playerId)?.ToDto(),
            Decor = _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors()),
            Toloka = toloka?.ToDto(),
            Market = _marketManager.GetMarket(playerId)?.ToDto(),
            Convoys = _convoyManager.GetConvoys(playerId).Select(x => x.ToDto()).ToArray(),
            Recap = _playerEventManager.TakeRecap(playerId, DateTimeHelper.GetNowDate()).ToDto(),
            Events = _playerEventManager.GetRecentEvents(playerId).Select(x => x.ToDto()).ToArray(),
            Goals = goals.ToDto(),
            VillageProfiles = _resourceManager.GetVillageProfileEffects().Select(x => x.ToDto()).ToArray(),
            Relocation = _relocationManager.GetState(playerId, villageLevel.Level).ToDto(),
        };

        return content;
    }
}
