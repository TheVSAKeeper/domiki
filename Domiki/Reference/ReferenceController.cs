using Domiki.Web.Core.Dto;
using Domiki.Web.Infrastructure;
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
    [Route("/Domiki/GetResourceTypes")]
    public Response<ResourceTypeDto[]> GetResourceTypes()
    {
        var content = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [HttpGet]
    [Route("/Domiki/GetReceipts")]
    public Response<ReceiptDto[]> GetReceipts()
    {
        var content = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray();
        return new(content);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("/System/Test")]
    public Response<DomikTypeDto[]> Test()
    {
        var content = _resourceManager.GetDomikTypes().Select(x => x.ToDto()).ToArray();
        return new(content);
    }
}
