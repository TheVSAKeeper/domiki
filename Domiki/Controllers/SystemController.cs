using Domiki.Web.Core;
using Domiki.Web.Core.Dto;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Controllers;

[ApiController]
public class SystemController : ControllerBase
{
    private readonly ILogger<DomikiController> _logger;
    private readonly DomikManager _domikManager;
    private readonly ResourceManager _resourceManager;

    public SystemController(ILogger<DomikiController> logger, DomikManager domikManager, ResourceManager resourceManager)
    {
        _logger = logger;
        _domikManager = domikManager;
        _resourceManager = resourceManager;
    }

    [HttpGet]
    [Route("/System/Test")]
    public Response<DomikTypeDto[]> GetDomikTypes()
    {
        var content = _resourceManager.GetDomikTypes().Select(x => x.ToDto()).ToArray();
        return new(content);
    }
}
