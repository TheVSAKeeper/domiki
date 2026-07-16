using Domiki.Web.Core;
using Domiki.Web.Infrastructure;
using Domiki.Web.Workers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Workers;

public class WorkerController : GameControllerBase
{
    private readonly WorkerManager _workerManager;

    public WorkerController(DomikManager domikManager, WorkerManager workerManager)
        : base(domikManager)
    {
        _workerManager = workerManager;
    }

    [HttpGet]
    [Route("/Domiki/GetWorkers")]
    public WorkerDto[] GetWorkers()
    {
        var playerId = GetPlayerId();

        return _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray();
    }
}
