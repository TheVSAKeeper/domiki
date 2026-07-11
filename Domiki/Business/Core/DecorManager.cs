using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class DecorManager
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly ResourceManager _resourceManager;
        private readonly PlayerResourceManager _playerResourceManager;

        public DecorManager(Data.UnitOfWork uow, Data.ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager)
        {
            _context = context;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
        }

        public DecorState GetDecor(int playerId)
        {
            var types = _resourceManager.GetDecorTypes();
            var owned = _context.PlayerDecors
                .Where(x => x.PlayerId == playerId)
                .Select(x => new PlayerDecor { DecorTypeId = x.DecorTypeId, Count = x.Count })
                .ToArray();

            return new DecorState
            {
                Types = types,
                Owned = owned,
                Comfort = DecorCalculator.GetComfort(owned, types),
            };
        }

        public void BuyDecor(int playerId, int decorTypeId)
        {
            _playerResourceManager.LockDbPlayerRow(playerId);

            var type = _resourceManager.GetDecorTypes().FirstOrDefault(x => x.Id == decorTypeId);
            if (type == null)
            {
                throw new BusinessException("Декор не найден");
            }

            if (!type.IsPurchasable)
            {
                throw new BusinessException("Этот декор нельзя купить");
            }

            _playerResourceManager.WriteOffResources(playerId, type.Cost);
            GrantDecor(playerId, decorTypeId, 1);
        }

        public void GrantDecor(int playerId, int decorTypeId, int count)
        {
            var decor = _context.PlayerDecors.Local.FirstOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId)
                ?? _context.PlayerDecors.FirstOrDefault(x => x.PlayerId == playerId && x.DecorTypeId == decorTypeId);
            if (decor == null)
            {
                decor = new Data.PlayerDecor { PlayerId = playerId, DecorTypeId = decorTypeId };
                _context.PlayerDecors.Add(decor);
            }

            decor.Count += count;
        }
    }
}
