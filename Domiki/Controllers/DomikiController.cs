using Domiki.Web.Activities;
using Domiki.Web.Activities.Dto;
using Domiki.Web.Core;
using Domiki.Web.Core.Dto;
using Domiki.Web.Economy;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Dto;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Dto;
using Domiki.Web.Village;
using Domiki.Web.Village.Dto;
using Domiki.Web.Workers;
using Domiki.Web.Workers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Web.Controllers;

[Authorize]
[ApiController]
public class DomikiController : ControllerBase
{
    private readonly ILogger<DomikiController> _logger;
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;
    private readonly OrderManager _orderManager;
    private readonly WorkerManager _workerManager;
    private readonly WeatherManager _weatherManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly BlueprintManager _blueprintManager;
    private readonly ExpeditionManager _expeditionManager;
    private readonly DecorManager _decorManager;
    private readonly TolokaManager _tolokaManager;
    private readonly MarketManager _marketManager;
    private readonly WorldManager _worldManager;
    private readonly SeasonManager _seasonManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly GoalManager _goalManager;

    public DomikiController(ILogger<DomikiController> logger, DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WorkerManager workerManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, ExpeditionManager expeditionManager, DecorManager decorManager, TolokaManager tolokaManager, MarketManager marketManager, WorldManager worldManager, SeasonManager seasonManager, PlayerEventManager playerEventManager, GoalManager goalManager)
    {
        _logger = logger;
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _orderManager = orderManager;
        _workerManager = workerManager;
        _weatherManager = weatherManager;
        _villageLevelCalculator = villageLevelCalculator;
        _blueprintManager = blueprintManager;
        _expeditionManager = expeditionManager;
        _decorManager = decorManager;
        _tolokaManager = tolokaManager;
        _marketManager = marketManager;
        _worldManager = worldManager;
        _seasonManager = seasonManager;
        _playerEventManager = playerEventManager;
        _goalManager = goalManager;
    }

    [HttpGet]
    [Route("/Domiki/GetGameState")]
    public Response<GameStateDto> GetGameState()
    {
        var playerId = GetPlayerId();
        var goals = _goalManager.GetGoalsState(playerId);
        var blueprints = _resourceManager.GetBlueprints();

        var content = new GameStateDto
        {
            DomikTypes = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray(),
            ResourceTypes = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray(),
            Receipts = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray(),
            Domiks = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray(),
            Resources = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray(),
            Orders = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray(),
            Reputation = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray(),
            Blueprints = _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray(),
            Village = _domikManager.GetVillage(playerId).ToDto(),
            VillageLevel = _villageLevelCalculator.GetLevel(playerId).ToDto(),
            Workers = _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray(),
            PurchaseAvailableDomiks = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id, x.NextCountGateLevel)).ToArray(),
            Weather = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto(),
            Expeditions = _expeditionManager.GetExpeditions(playerId)?.ToDto(),
            Decor = _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors()),
            Toloka = _tolokaManager.GetToloka(DateTimeHelper.GetNowDate(), playerId)?.ToDto(),
            Market = _marketManager.GetMarket(playerId)?.ToDto(),
            Recap = _playerEventManager.TakeRecap(playerId, DateTimeHelper.GetNowDate()).ToDto(),
            Events = _playerEventManager.GetRecentEvents(playerId).Select(x => x.ToDto()).ToArray(),
            Goals = goals.ToDto(),
        };

        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetDomikTypes")] // todo разобраться с роут префиксом
    public Response<DomikTypeDto[]> GetDomikTypes()
    {
        var blueprints = _resourceManager.GetBlueprints();
        var content = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetModificatorTypes")] // todo объеденить системные методы в один GetData, где возвращать системную инфу о домиках ресурсах и прочем
    public Response<ModificatorTypeDto[]> GetModificatorTypes()
    {
        var content = _resourceManager.GetModificatorTypes().Select(x => x.ToDto()).ToArray();
        return new(content);
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
    [Route("/Domiki/GetVillage")]
    public Response<VillageDto> GetVillage()
    {
        var playerId = GetPlayerId();

        var content = _domikManager.GetVillage(playerId).ToDto();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetWorld")]
    public Response<WorldDto> GetWorld()
    {
        var playerId = GetPlayerId();

        var content = _worldManager.GetWorld(playerId).ToDto();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/VisitVillage/{playerId}")]
    public Response<VillageVisitDto> VisitVillage(int playerId)
    {
        var content = _worldManager.VisitVillage(playerId).ToDto();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetSeason")]
    public Response<SeasonDto> GetSeason()
    {
        var content = _seasonManager.GetCurrentSeason(DateTimeHelper.GetNowDate()).ToDto();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/SetVillage")]
    public Response SetVillage([FromBody] SetVillageDto request)
    {
        var playerId = GetPlayerId();
        _domikManager.SetVillageIdentity(playerId, request?.Name, request?.CrestIcon ?? -1, request?.CrestColor ?? -1);
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/SetFeedWorkers")]
    public Response SetFeedWorkers([FromBody] SetFeedWorkersDto request)
    {
        var playerId = GetPlayerId();
        _domikManager.SetFeedWorkers(playerId, request?.Enabled ?? false);
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

    [HttpGet]
    [Route("/Domiki/GetResourceTypes")]
    public Response<ResourceTypeDto[]> GetResourceTypes()
    {
        var content = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetWorkers")]
    public Response<WorkerDto[]> GetWorkers()
    {
        var playerId = GetPlayerId();

        var content = _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray();
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

    [HttpGet]
    [Route("/Domiki/GetReceipts")]
    public Response<ReceiptDto[]> GetReceipts()
    {
        var content = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetOrders")]
    public Response<OrderDto[]> GetOrders()
    {
        var playerId = GetPlayerId();

        var content = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/CompleteOrder/{orderId}")]
    public Response CompleteOrder(int orderId)
    {
        var playerId = GetPlayerId();
        _orderManager.CompleteOrder(playerId, orderId);
        return new()
            { Type = ResponseType.Success };
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
    [Route("/Domiki/GetReputation")]
    public Response<NeighborReputationDto[]> GetReputation()
    {
        var playerId = GetPlayerId();

        var content = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetVillageLevel")]
    public Response<VillageLevelDto> GetVillageLevel()
    {
        var playerId = GetPlayerId();

        var content = _villageLevelCalculator.GetLevel(playerId).ToDto();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetWeather")]
    public Response<WeatherStateDto> GetWeather()
    {
        var content = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetDecor")]
    public Response<DecorStateDto> GetDecor()
    {
        var playerId = GetPlayerId();

        var content = _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors());
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/BuyDecor/{decorTypeId}")]
    public Response BuyDecor(int decorTypeId)
    {
        var playerId = GetPlayerId();
        _decorManager.BuyDecor(playerId, decorTypeId);
        return new()
            { Type = ResponseType.Success };
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
    [Route("/Domiki/GetMarket")]
    public Response<MarketStateDto> GetMarket()
    {
        var playerId = GetPlayerId();

        var content = _marketManager.GetMarket(playerId)?.ToDto();
        return new(content);
    }

    [HttpPost]
    [Route("/Domiki/PostLot")]
    public Response PostLot([FromQuery] int giveResourceTypeId, [FromQuery] int giveValue, [FromQuery] int wantResourceTypeId, [FromQuery] int wantValue)
    {
        var playerId = GetPlayerId();
        _marketManager.PostLot(playerId, giveResourceTypeId, giveValue, wantResourceTypeId, wantValue, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/AcceptLot/{lotId}")]
    public Response AcceptLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.AcceptLot(playerId, lotId, DateTimeHelper.GetNowDate());
        return new()
            { Type = ResponseType.Success };
    }

    [HttpPost]
    [Route("/Domiki/CancelLot/{lotId}")]
    public Response CancelLot(int lotId)
    {
        var playerId = GetPlayerId();
        _marketManager.CancelLot(playerId, lotId, DateTimeHelper.GetNowDate());
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

    private int GetPlayerId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var playerId = _domikManager.GetPlayerId(userId);
        return playerId;
    }
}
