using Domiki.Web.Core.Dto;
using Domiki.Web.Reference.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Reference;

[Authorize]
[ApiController]
public class ReferenceController : ControllerBase
{
    private readonly ResourceManager _resourceManager;

    public ReferenceController(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    [HttpGet]
    [Route("/Domiki/GetDomikTypes")] // todo разобраться с роут префиксом
    public DomikTypeDto[] GetDomikTypes()
    {
        var blueprints = _resourceManager.GetBlueprints();
        return _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetModificatorTypes")] // todo объеденить системные методы в один GetData, где возвращать системную инфу о домиках ресурсах и прочем
    public ModificatorTypeDto[] GetModificatorTypes()
    {
        return _resourceManager.GetModificatorTypes().Select(x => x.ToDto()).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetResourceTypes")]
    public ResourceTypeDto[] GetResourceTypes()
    {
        return _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray();
    }

    [HttpGet]
    [Route("/Domiki/GetReceipts")]
    public ReceiptDto[] GetReceipts()
    {
        return _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray();
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("/System/Test")]
    public DomikTypeDto[] Test()
    {
        return _resourceManager.GetDomikTypes().Select(x => x.ToDto()).ToArray();
    }
}
