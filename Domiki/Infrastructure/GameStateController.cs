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
    private readonly WeatherManager _weatherManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly DecorManager _decorManager;
    private readonly TolokaManager _tolokaManager;
    private readonly ConvoyManager _convoyManager;
    private readonly GiftManager _giftManager;
    private readonly PlayerEventManager _playerEventManager;
    private readonly WorkerMilestoneManager _workerMilestoneManager;
    private readonly RelocationManager _relocationManager;
    private readonly WikiFactsProvider _wikiFactsProvider;
    private readonly GameStateProjector _projector;

    public GameStateController(DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, DecorManager decorManager, TolokaManager tolokaManager, ConvoyManager convoyManager, GiftManager giftManager, PlayerEventManager playerEventManager, WorkerMilestoneManager workerMilestoneManager, RelocationManager relocationManager, WikiFactsProvider wikiFactsProvider, GameStateProjector projector)
        : base(domikManager)
    {
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _orderManager = orderManager;
        _weatherManager = weatherManager;
        _villageLevelCalculator = villageLevelCalculator;
        _decorManager = decorManager;
        _tolokaManager = tolokaManager;
        _convoyManager = convoyManager;
        _giftManager = giftManager;
        _playerEventManager = playerEventManager;
        _workerMilestoneManager = workerMilestoneManager;
        _relocationManager = relocationManager;
        _wikiFactsProvider = wikiFactsProvider;
        _projector = projector;
    }

    /// <summary>
    /// Снимок состояния игры вместе с одноразовыми выдачами.
    /// </summary>
    /// <returns>Полное состояние игрока: справочники, двор, деревня, витрина «Пока вас не было».</returns>
    /// <remarks>
    /// Сборку снимка держит <see cref="GameStateProjector"/>, здесь остаётся то, что игрок получает один раз: гостинец,
    /// веха трудяги, дельта вклада в толоку и витрина «Пока вас не было». Доска заказов достраивается тоже здесь
    /// (<see cref="OrderManager.GetOrders"/>) – проектор её только читает.
    /// </remarks>
    [HttpGet]
    [Route("/Domiki/GetGameState")]
    public GameStateDto GetGameState()
    {
        var playerId = GetPlayerId();
        var now = DateTimeHelper.GetNowDate();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId);
        _giftManager.TryGrantGift(playerId, now);
        _workerMilestoneManager.TryGrantNext(playerId, villageLevel.Level, now);
        _orderManager.GetOrders(playerId);

        var toloka = _tolokaManager.GetToloka(now, playerId);
        if (toloka != null)
        {
            toloka.Progress = _tolokaManager.TakeProgress(playerId);
        }

        return _projector.Project(playerId) with
        {
            Toloka = toloka?.ToDto(),
            Recap = _playerEventManager.TakeRecap(playerId, now).ToDto(),
        };
    }

    /// <summary>
    /// Справочный срез состояния для внутриигрового справочника.
    /// </summary>
    /// <returns>Справочники, погода, обжитость, обозы, толока и уклад деревни игрока.</returns>
    /// <remarks>
    /// Отдельный от <see cref="GetGameState"/> экшен именно потому, что снимок состояния игры – не чистое чтение:
    /// он выдаёт витрину «Пока вас не было» и двигает курсор доставки событий
    /// (<see cref="PlayerEventManager.TakeRecap"/>), выдаёт подарок и веху трудяги, забирает дельту вклада в толоку
    /// (<see cref="Activities.TolokaManager.TakeProgress"/>) и продвигает наказы. Справочник открывают между делом,
    /// в том числе первым экраном после долгого отсутствия, поэтому здесь ничего из этого не вызывается.
    /// </remarks>
    [HttpGet]
    [Route("/Domiki/GetWikiState")]
    public WikiStateDto GetWikiState()
    {
        var playerId = GetPlayerId();
        var blueprints = _resourceManager.GetBlueprints();
        var villageLevel = _villageLevelCalculator.GetLevel(playerId);

        return new WikiStateDto
        {
            Facts = _wikiFactsProvider.GetFacts(),
            DomikTypes = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray(),
            ResourceTypes = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray(),
            Receipts = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray(),
            Weather = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto(),
            Decor = _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors()),
            VillageLevel = villageLevel.ToDto(),
            Convoys = _convoyManager.GetConvoys(playerId).Select(x => x.ToDto()).ToArray(),
            Toloka = _tolokaManager.GetToloka(DateTimeHelper.GetNowDate(), playerId)?.ToDto(),
            Village = _domikManager.GetVillage(playerId).ToDto(),
            VillageProfiles = _resourceManager.GetVillageProfileEffects().Select(x => x.ToDto()).ToArray(),
            Reputation = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray(),
            Relocation = _relocationManager.GetState(playerId, villageLevel.Level).ToDto(),
        };
    }
}
