using Domiki.Web;
using Domiki.Web.Business;
using Domiki.Web.Business.Core;
using Domiki.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Domiki.Controllers
{
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

        public DomikiController(ILogger<DomikiController> logger, DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WorkerManager workerManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator)
        {
            _logger = logger;
            _domikManager = domikManager;
            _resourceManager = resourceManager;
            _orderManager = orderManager;
            _workerManager = workerManager;
            _weatherManager = weatherManager;
            _villageLevelCalculator = villageLevelCalculator;
        }

        [HttpGet]
        [Route("/Domiki/GetDomikTypes")] // todo разобраться с роут префиксом
        public Response<DomikTypeDto[]> GetDomikTypes()
        {
            var content = _resourceManager.GetDomikTypes().Select(x => x.ToDto()).ToArray();
            return new Response<DomikTypeDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetModificatorTypes")] // todo объеденить системные методы в один GetData, где возвращать системную инфу о домиках ресурсах и прочем
        public Response<ModificatorTypeDto[]> GetModificatorTypes()
        {
            var content = _resourceManager.GetModificatorTypes().Select(x => x.ToDto()).ToArray();
            return new Response<ModificatorTypeDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetDomiks")]
        public Response<DomikDto[]> GetDomiks()
        {
            int playerId = GetPlayerId();

            var content = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<DomikDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetVillage")]
        public Response<VillageDto> GetVillage()
        {
            int playerId = GetPlayerId();

            var content = _domikManager.GetVillage(playerId).ToDto();
            return new Response<VillageDto>(content);
        }

        [HttpPost]
        [Route("/Domiki/SetVillage")]
        public Response SetVillage([FromBody] SetVillageDto request)
        {
            int playerId = GetPlayerId();
            _domikManager.SetVillageIdentity(playerId, request?.Name, request?.CrestIcon ?? -1, request?.CrestColor ?? -1);
            return new Response { Type = ResponseType.Success };
        }

        [HttpPost]
        [Route("/Domiki/UpgradeDomik/{id}")]
        public Response UpgradeDomik(int id)
        {
            int playerId = GetPlayerId();
            _domikManager.UpgradeDomik(playerId, id);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetPurchaseAvaialableDomiks")]
        public Response<DomikTypeDto[]> GetPurchaseAvaialableDomiks()
        {
            int playerId = GetPlayerId();
            var content = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount)).ToArray();
            return new Response<DomikTypeDto[]>(content);
        }

        [HttpPost]
        [Route("/Domiki/BuyDomik/{typeId}")]
        public Response BuyDomik(int typeId)
        {
            int playerId = GetPlayerId();
            _domikManager.BuyDomik(playerId, typeId);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetResourceTypes")]
        public Response<ResourceTypeDto[]> GetResourceTypes()
        {
            var content = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray();
            return new Response<ResourceTypeDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetWorkers")]
        public Response<WorkerDto[]> GetWorkers()
        {
            int playerId = GetPlayerId();

            var content = _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<WorkerDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetResources")]
        public Response<ResourceDto[]> GetResources()
        {
            int playerId = GetPlayerId();

            var content = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<ResourceDto[]>(content);
        }

        [HttpPost]
        [Route("/Domiki/StartManufacture/{domikId}/{receiptId}")]
        public Response StartManufacture(int domikId, int receiptId, [FromQuery] bool useOptional = false, [FromQuery] int[] workerIds = null)
        {
            int playerId = GetPlayerId();
            _domikManager.StartManufacture(playerId, domikId, receiptId, useOptional, workerIds);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetReceipts")]
        public Response<ReceiptDto[]> GetReceipts()
        {
            var content = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray();
            return new Response<ReceiptDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetOrders")]
        public Response<OrderDto[]> GetOrders()
        {
            int playerId = GetPlayerId();

            var content = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<OrderDto[]>(content);
        }

        [HttpPost]
        [Route("/Domiki/CompleteOrder/{orderId}")]
        public Response CompleteOrder(int orderId)
        {
            int playerId = GetPlayerId();
            _orderManager.CompleteOrder(playerId, orderId);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetReputation")]
        public Response<NeighborReputationDto[]> GetReputation()
        {
            int playerId = GetPlayerId();

            var content = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<NeighborReputationDto[]>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetVillageLevel")]
        public Response<VillageLevelDto> GetVillageLevel()
        {
            int playerId = GetPlayerId();

            var content = _villageLevelCalculator.GetLevel(playerId).ToDto();
            return new Response<VillageLevelDto>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetWeather")]
        public Response<WeatherStateDto> GetWeather()
        {
            var content = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto();
            return new Response<WeatherStateDto>(content);
        }

        private int GetPlayerId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var playerId = _domikManager.GetPlayerId(userId);
            return playerId;
        }
    }
}