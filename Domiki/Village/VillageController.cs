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

    public VillageController(DomikManager domikManager, ResourceManager resourceManager, WorldManager worldManager, SeasonManager seasonManager, VillageLevelCalculator villageLevelCalculator, WeatherManager weatherManager, DecorManager decorManager)
        : base(domikManager)
    {
        _domikManager = domikManager;
        _resourceManager = resourceManager;
        _worldManager = worldManager;
        _seasonManager = seasonManager;
        _villageLevelCalculator = villageLevelCalculator;
        _weatherManager = weatherManager;
        _decorManager = decorManager;
    }

    [HttpGet]
    [Route("/Domiki/GetVillage")]
    public Response<VillageDto> GetVillage()
    {
        var playerId = GetPlayerId();

        var content = _domikManager.GetVillage(playerId).ToDto();
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

    [HttpGet]
    [Route("/Domiki/GetVillageLevel")]
    public Response<VillageLevelDto> GetVillageLevel()
    {
        var playerId = GetPlayerId();

        var content = _villageLevelCalculator.GetLevel(playerId).ToDto();
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
}
