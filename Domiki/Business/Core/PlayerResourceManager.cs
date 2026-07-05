using Domiki.Web.Business.Models;

namespace Domiki.Web.Business.Core
{
    public class PlayerResourceManager
    {
        private Data.ApplicationDbContext _context;
        private ResourceManager _resourceManager;

        public PlayerResourceManager(Data.ApplicationDbContext context, ResourceManager resourceManager)
        {
            _context = context;
            _resourceManager = resourceManager;
        }

        public void LockDbPlayerRow(int playerId)
        {
            _context.Players.First(x => x.Id == playerId).Version = Guid.NewGuid();
        }

        public void WriteOffResources(int playerId, Resource[] resources)
        {
            var dbResources = _context.Resources.Where(x => x.PlayerId == playerId).ToArray();
            var resourceTypes = _resourceManager.GetResourceTypes();
            foreach (var needResource in resources)
            {
                var dbResource = dbResources.FirstOrDefault(x => x.TypeId == needResource.Type.Id);
                if (dbResource == null)
                {
                    throw new BusinessException("Недостаточно " + GetResourceName(needResource, resourceTypes));
                }
                dbResource.Value -= needResource.Value;
                if (dbResource.Value < 0)
                {
                    throw new BusinessException("Недостаточно " + GetResourceName(needResource, resourceTypes));
                }
            }
        }

        public void GrantResource(int playerId, int typeId, int value)
        {
            if (value == 0)
            {
                return;
            }

            var dbResource = _context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
            if (dbResource == null)
            {
                dbResource = new Data.Resource { PlayerId = playerId, TypeId = typeId };
                _context.Resources.Add(dbResource);
            }

            dbResource.Value += value;
        }

        private string GetResourceName(Resource resource, ResourceType[] resourceTypes)
        {
            return resource.Type.Name ?? resourceTypes.First(x => x.Id == resource.Type.Id).Name;
        }
    }
}
