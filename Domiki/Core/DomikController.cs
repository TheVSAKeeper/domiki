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
    public DomikDto[] GetDomiks()
    {
        var playerId = GetPlayerId();

        return _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetResources")]
    public ResourceDto[] GetResources()
    {
        var playerId = GetPlayerId();

        return _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetPurchaseAvaialableDomiks")]
    public DomikTypeDto[] GetPurchaseAvaialableDomiks()
    {
        var playerId = GetPlayerId();
        var blueprints = _resourceManager.GetBlueprints();
        return _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id, x.NextCountGateLevel)).ToArray();
    }

    [HttpPost]
    [Route("/Domiki/BuyDomik/{typeId}")]
    public void BuyDomik(int typeId)
    {
        var playerId = GetPlayerId();
        _domikManager.BuyDomik(playerId, typeId);
    }

    [HttpPost]
    [Route("/Domiki/UpgradeDomik/{id}")]
    public void UpgradeDomik(int id)
    {
        var playerId = GetPlayerId();
        _domikManager.UpgradeDomik(playerId, id);
    }

    /// <summary>
    /// Помечает домик задумкой – целью, на улучшение которой игрок копит, – либо снимает пометку.
    /// </summary>
    /// <param name="domikId">Номер домика в пределах игрока; не задан – задумка снимается.</param>
    [HttpPost]
    [Route("/Domiki/SetUpgradeIntent")]
    public void SetUpgradeIntent([FromQuery] int? domikId = null)
    {
        var playerId = GetPlayerId();
        _domikManager.SetUpgradeIntent(playerId, domikId);
    }

    [HttpPost]
    [Route("/Domiki/HurryDomik/{id}")]
    public void HurryDomik(int id)
    {
        var playerId = GetPlayerId();
        _domikManager.HurryDomik(playerId, id);
    }

    [HttpPost]
    [Route("/Domiki/StartManufacture/{domikId}/{receiptId}")]
    public void StartManufacture(int domikId, int receiptId, [FromQuery] bool useOptional = false, [FromQuery] int[]? workerIds = null, [FromQuery] bool autoRepeat = false)
    {
        var playerId = GetPlayerId();
        _domikManager.StartManufacture(playerId, domikId, receiptId, useOptional, workerIds, autoRepeat);
    }

    /// <summary>
    /// Завершает смену немедленно за золото.
    /// </summary>
    /// <param name="manufactureId">Идентификатор смены.</param>
    /// <param name="confirmed">
    /// Игрок подтвердил цену выше <see cref="DomikManager.InstaFinishConfirmGold"/>; без параметра считается неподтверждённой.
    /// </param>
    [HttpPost]
    [Route("/Domiki/HurryManufacture/{manufactureId}")]
    public void HurryManufacture(int manufactureId, [FromQuery] bool confirmed = false)
    {
        var playerId = GetPlayerId();
        _domikManager.HurryManufacture(playerId, manufactureId, confirmed);
    }

    [HttpPost]
    [Route("/Domiki/SetManufactureAutoRepeat/{manufactureId}")]
    public void SetManufactureAutoRepeat(int manufactureId, [FromQuery] bool autoRepeat)
    {
        var playerId = GetPlayerId();
        _domikManager.SetManufactureAutoRepeat(playerId, manufactureId, autoRepeat);
    }

    /// <summary>
    /// Назначает наряду меру или снимает её.
    /// </summary>
    /// <param name="manufactureId">Идентификатор смены, на которой стоит наряд.</param>
    /// <param name="resourceTypeId">Тип ресурса меры; без него мера снимается.</param>
    /// <param name="value">Сколько единиц набрать; без него мера снимается.</param>
    [HttpPost]
    [Route("/Domiki/SetManufactureMeasure/{manufactureId}")]
    public void SetManufactureMeasure(int manufactureId, [FromQuery] int? resourceTypeId = null, [FromQuery] int? value = null)
    {
        var playerId = GetPlayerId();
        _domikManager.SetManufactureMeasure(playerId, manufactureId, resourceTypeId, value);
    }
}
