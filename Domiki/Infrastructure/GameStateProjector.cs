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

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Собирает снимок состояния игры, ничего у игрока не потребляя.
/// </summary>
/// <remarks>
/// Четыре одноразовые выдачи – гостинец (<see cref="GiftManager.TryGrantGift"/>), веха трудяги
/// (<see cref="WorkerMilestoneManager.TryGrantNext"/>), дельта вклада в толоку (<see cref="TolokaManager.TakeProgress"/>)
/// и витрина «Пока вас не было» (<see cref="PlayerEventManager.TakeRecap"/>) – остаются в
/// <see cref="GameStateController.GetGameState"/>: снимок, отданный в ответе на действие игрока, не имеет права их съесть.
/// Ленивые достройки (трудяги, чертежи, доска заказов) здесь допускаются – они ничего не выдают, а лишь доводят состояние
/// до нормы, и всё равно случились бы при следующем чтении. Заказы читаются <see cref="OrderManager.ReadOrders"/> без
/// достройки доски: она создаёт заказы со случайным соседом, и в ответе на действие это лишняя неожиданность.
/// </remarks>
public class GameStateProjector
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
    private readonly PlayerEventManager _playerEventManager;
    private readonly GoalManager _goalManager;
    private readonly ErrandManager _errandManager;
    private readonly IncidentManager _incidentManager;
    private readonly ElderHouseManager _elderHouseManager;
    private readonly RelocationManager _relocationManager;

    public GameStateProjector(DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WorkerManager workerManager, TavernManager tavernManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, ExpeditionManager expeditionManager, DecorManager decorManager, TolokaManager tolokaManager, MarketManager marketManager, ConvoyManager convoyManager, PlayerEventManager playerEventManager, GoalManager goalManager, ErrandManager errandManager, IncidentManager incidentManager, ElderHouseManager elderHouseManager, RelocationManager relocationManager)
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
        _playerEventManager = playerEventManager;
        _goalManager = goalManager;
        _errandManager = errandManager;
        _incidentManager = incidentManager;
        _elderHouseManager = elderHouseManager;
        _relocationManager = relocationManager;
    }

    /// <summary>
    /// Снимок состояния игрока без одноразовых выдач.
    /// </summary>
    /// <param name="playerId">Игрок, чьё состояние собирается.</param>
    /// <returns>Полный снимок: витрина «Пока вас не было» пуста, дельта вклада в толоку нулевая.</returns>
    /// <remarks>
    /// Продвижение стартовых целей остаётся здесь: <see cref="GoalManager.GetGoalsState"/> закрывает выполненные цели
    /// и заводит о них событие, поэтому вызывается до чтения ленты событий – иначе событие о цели опоздает на снимок.
    /// </remarks>
    public GameStateDto Project(int playerId)
    {
        var goals = _goalManager.GetGoalsState(playerId);
        var blueprints = _resourceManager.GetBlueprints();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId);
        var now = DateTimeHelper.GetNowDate();

        return new GameStateDto
        {
            PlayerId = playerId,
            DomikTypes = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray(),
            ResourceTypes = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray(),
            Receipts = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray(),
            Domiks = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray(),
            Resources = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray(),
            Orders = _orderManager.ReadOrders(playerId).Select(x => x.ToDto()).ToArray(),
            OrderBoardSize = _orderManager.GetPlayerBoardSize(playerId),
            OrderFreeConcession = _orderManager.IsFreeConcessionAvailable(playerId),
            Reputation = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray(),
            Errands = _errandManager.GetAll(playerId).Select(x => x.ToDto()).ToArray(),
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
            IntentDomikId = _domikManager.GetUpgradeIntent(playerId),
            IntentReserves = _elderHouseManager.GetIntentReserves(playerId).Select(x => x.ToDto()).ToArray(),
            SickTypes = _resourceManager.GetSickTypes().Select(x => x.ToDto()).ToArray(),
            PurchaseAvailableDomiks = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id, x.NextCountGateLevel)).ToArray(),
            Weather = _weatherManager.GetWeather(now).ToDto(),
            Expeditions = _expeditionManager.GetExpeditions(playerId)?.ToDto(),
            Decor = _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors()),
            Toloka = _tolokaManager.GetToloka(now, playerId)?.ToDto(),
            Market = _marketManager.GetMarket(playerId)?.ToDto(),
            Convoys = _convoyManager.GetConvoys(playerId).Select(x => x.ToDto()).ToArray(),
            Recap = EmptyRecap,
            Events = _playerEventManager.GetRecentEvents(playerId).Select(x => x.ToDto()).ToArray(),
            Goals = goals.ToDto(),
            VillageProfiles = _resourceManager.GetVillageProfileEffects().Select(x => x.ToDto()).ToArray(),
            Relocation = _relocationManager.GetState(playerId, villageLevel.Level).ToDto(),
        };
    }

    private static RecapDto EmptyRecap => new() { AwaySeconds = 0, Events = [] };
}
