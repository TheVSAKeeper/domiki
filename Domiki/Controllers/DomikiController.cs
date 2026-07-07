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
        private readonly BlueprintManager _blueprintManager;
        private readonly ExpeditionManager _expeditionManager;
        private readonly DecorManager _decorManager;

        public DomikiController(ILogger<DomikiController> logger, DomikManager domikManager, ResourceManager resourceManager, OrderManager orderManager, WorkerManager workerManager, WeatherManager weatherManager, VillageLevelCalculator villageLevelCalculator, BlueprintManager blueprintManager, ExpeditionManager expeditionManager, DecorManager decorManager)
        {
            _logger = logger;
            _domikManager = domikManager;
            _resourceManager = resourceManager;
            _orderManager = orderManager;
            _workerManager = workerManager;
            _weatherManager = weatherManager;
            _villageLevelCalculator = villageLevelCalculator;
            _blueprintManager = blueprintManager;
            _expeditionManager = expeditionManager;
            _decorManager = decorManager;
        }

        [HttpGet]
        [Route("/Domiki/GetGameState")]
        public Response<GameStateDto> GetGameState()
        {
            int playerId = GetPlayerId();
            var blueprints = _resourceManager.GetBlueprints();

            var content = new GameStateDto
            {
                DomikTypes = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray(),
                ResourceTypes = _resourceManager.GetResourceTypes().Select(x => x.ToDto()).ToArray(),
                Receipts = _resourceManager.GetReceipts().Select(x => x.ToDto()).ToArray(),
                Domiks = _domikManager.GetDomiks(playerId).Select(x => x.ToDto()).ToArray(),
                Resources = _domikManager.GetResources(playerId).Select(x => x.ToDto()).ToArray(),
                Orders = _orderManager.GetOrders(playerId).Select(x => x.ToDto()).ToArray(),
                Reputation = _orderManager.GetReputation(playerId).Select(x => x.ToDto()).ToArray(),
                Blueprints = _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray(),
                Village = _domikManager.GetVillage(playerId).ToDto(),
                VillageLevel = _villageLevelCalculator.GetLevel(playerId).ToDto(),
                Workers = _workerManager.GetWorkers(playerId).Select(x => x.ToDto()).ToArray(),
                PurchaseAvailableDomiks = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id)).ToArray(),
                Weather = _weatherManager.GetWeather(DateTimeHelper.GetNowDate()).ToDto(),
                Expeditions = _expeditionManager.GetExpeditions(playerId).ToDto(),
                Decor = _decorManager.GetDecor(playerId).ToDto(),
            };
            return new Response<GameStateDto>(content);
        }

        [HttpGet]
        [Route("/Domiki/GetDomikTypes")] // todo разобраться с роут префиксом
        public Response<DomikTypeDto[]> GetDomikTypes()
        {
            var blueprints = _resourceManager.GetBlueprints();
            var content = _resourceManager.GetDomikTypes().Select(x => x.ToDto(blueprintId: blueprints.FirstOrDefault(b => b.DomikTypeId == x.Id)?.Id)).ToArray();
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

        [HttpPost]
        [Route("/Domiki/HurryDomik/{id}")]
        public Response HurryDomik(int id)
        {
            int playerId = GetPlayerId();
            _domikManager.HurryDomik(playerId, id);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetPurchaseAvaialableDomiks")]
        public Response<DomikTypeDto[]> GetPurchaseAvaialableDomiks()
        {
            int playerId = GetPlayerId();
            var blueprints = _resourceManager.GetBlueprints();
            var content = _domikManager.GetPurchaseAvailableDomiks(playerId).Select(x => x.Type.ToDto(x.AvailableCount, blueprints.FirstOrDefault(b => b.DomikTypeId == x.Type.Id)?.Id)).ToArray();
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

        [HttpPost]
        [Route("/Domiki/HurryManufacture/{manufactureId}")]
        public Response HurryManufacture(int manufactureId)
        {
            int playerId = GetPlayerId();
            _domikManager.HurryManufacture(playerId, manufactureId);
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
        [Route("/Domiki/GetBlueprints")]
        public Response<BlueprintDto[]> GetBlueprints()
        {
            int playerId = GetPlayerId();

            var content = _blueprintManager.GetBlueprints(playerId).Select(x => x.ToDto()).ToArray();
            return new Response<BlueprintDto[]>(content);
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

        [HttpGet]
        [Route("/Domiki/GetDecor")]
        public Response<DecorStateDto> GetDecor()
        {
            int playerId = GetPlayerId();

            var content = _decorManager.GetDecor(playerId).ToDto();
            return new Response<DecorStateDto>(content);
        }

        [HttpPost]
        [Route("/Domiki/BuyDecor/{decorTypeId}")]
        public Response BuyDecor(int decorTypeId)
        {
            int playerId = GetPlayerId();
            _decorManager.BuyDecor(playerId, decorTypeId);
            return new Response { Type = ResponseType.Success };
        }

        [HttpGet]
        [Route("/Domiki/GetExpeditions")]
        public Response<ExpeditionStateDto> GetExpeditions()
        {
            int playerId = GetPlayerId();

            var content = _expeditionManager.GetExpeditions(playerId).ToDto();
            return new Response<ExpeditionStateDto>(content);
        }

        [HttpPost]
        [Route("/Domiki/StartExpedition/{expeditionTypeId}")]
        public Response StartExpedition(int expeditionTypeId)
        {
            int playerId = GetPlayerId();
            _expeditionManager.StartExpedition(playerId, expeditionTypeId);
            return new Response { Type = ResponseType.Success };
        }

        private int GetPlayerId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var playerId = _domikManager.GetPlayerId(userId);
            return playerId;
        }
    }
}