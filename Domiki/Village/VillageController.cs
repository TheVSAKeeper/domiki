using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Village;

public class VillageController : GameControllerBase
{
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;
    private readonly WorldManager _worldManager;
    private readonly SeasonManager _seasonManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly WeatherManager _weatherManager;
    private readonly DecorManager _decorManager;
    private readonly GuestbookManager _guestbookManager;

    public VillageController(DomikManager domikManager, ResourceManager resourceManager, WorldManager worldManager, SeasonManager seasonManager, VillageLevelCalculator villageLevelCalculator, WeatherManager weatherManager, DecorManager decorManager, GuestbookManager guestbookManager)
        : base(domikManager)
    {
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _worldManager = worldManager;
        _seasonManager = seasonManager;
        _villageLevelCalculator = villageLevelCalculator;
        _weatherManager = weatherManager;
        _decorManager = decorManager;
        _guestbookManager = guestbookManager;
    }

    [HttpGet]
    [Route("/Domiki/GetVillage")]
    public VillageDto GetVillage()
    {
        var playerId = GetPlayerId();

        return _domikManager.GetVillage(playerId).ToDto();
    }

    [HttpPost]
    [Route("/Domiki/SetVillage")]
    public void SetVillage([FromBody] SetVillageDto request)
    {
        var playerId = GetPlayerId();
        _domikManager.SetVillageIdentity(playerId, request?.Name, request?.CrestIcon ?? -1, request?.CrestColor ?? -1);
    }

    [HttpPost]
    [Route("/Domiki/SetFeedWorkers")]
    public void SetFeedWorkers([FromBody] SetFeedWorkersDto request)
    {
        var playerId = GetPlayerId();
        _domikManager.SetFeedWorkers(playerId, request?.Enabled ?? false);
    }

    [HttpGet]
    [Route("/Domiki/GetVillageLevel")]
    public VillageLevelDto GetVillageLevel()
    {
        var playerId = GetPlayerId();

        return _villageLevelCalculator.GetLevel(playerId).ToDto();
    }

    [HttpGet]
    [Route("/Domiki/GetWorld")]
    public WorldDto GetWorld()
    {
        var playerId = GetPlayerId();

        return _worldManager.GetWorld(playerId).ToDto();
    }

    /// <summary>
    /// Визит в чужую деревню: витрина деревни, след визита и книга гостей глазами гостя.
    /// </summary>
    /// <param name="playerId">Id игрока-хозяина посещаемой деревни.</param>
    /// <returns>Витрина деревни вместе с книгой гостей глазами гостя.</returns>
    [HttpGet]
    [Route("/Domiki/VisitVillage/{playerId}")]
    public VillageVisitDto VisitVillage(int playerId)
    {
        var guestId = GetPlayerId();
        var date = DateTimeHelper.GetNowDate();

        var visit = _worldManager.VisitVillage(playerId);
        _guestbookManager.RecordVisit(guestId, playerId, date);
        var guestbook = _guestbookManager.GetVisitGuestbook(playerId, guestId, date);

        return visit.ToDto(guestbook);
    }

    /// <summary>
    /// Запись выбранной фразы в книгу гостей деревни хозяина.
    /// </summary>
    /// <param name="hostPlayerId">Id игрока-хозяина книги.</param>
    /// <param name="phraseId">Фраза из справочника книги гостей.</param>
    [HttpPost]
    [Route("/Domiki/LeaveGuestbookEntry/{hostPlayerId}")]
    public void LeaveGuestbookEntry(int hostPlayerId, [FromQuery] int phraseId)
    {
        var guestId = GetPlayerId();
        _guestbookManager.LeaveEntry(guestId, hostPlayerId, phraseId, DateTimeHelper.GetNowDate());
    }

    /// <summary>
    /// Книга гостей собственной деревни: визиты за сезон и лента записей.
    /// </summary>
    /// <returns>Книга гостей собственной деревни.</returns>
    [HttpGet]
    [Route("/Domiki/GetGuestbook")]
    public GuestbookDto GetGuestbook()
    {
        var playerId = GetPlayerId();

        return _guestbookManager.GetGuestbook(playerId, DateTimeHelper.GetNowDate()).ToDto();
    }

    [HttpGet]
    [Route("/Domiki/GetSeason")]
    public SeasonDto GetSeason()
    {
        return _seasonManager.GetCurrentSeason(DateTimeHelper.GetNowDate()).ToDto();
    }

    [HttpGet]
    [Route("/Domiki/GetWeather")]
    public WeatherStateDto GetWeather()
    {
        return _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto();
    }

    [HttpGet]
    [Route("/Domiki/GetDecor")]
    public DecorStateDto GetDecor()
    {
        var playerId = GetPlayerId();

        return _decorManager.GetDecor(playerId).ToDto(_resourceManager.GetNeighbors());
    }

    [HttpPost]
    [Route("/Domiki/BuyDecor/{decorTypeId}")]
    public void BuyDecor(int decorTypeId)
    {
        var playerId = GetPlayerId();
        _decorManager.BuyDecor(playerId, decorTypeId);
    }
}
