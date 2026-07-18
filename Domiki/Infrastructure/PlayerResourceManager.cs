using Domiki.Web.Data;
using Domiki.Web.Reference;
using Domiki.Web.Reference.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Infrastructure;

public class PlayerResourceManager
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;

    public PlayerResourceManager(UnitOfWork uow, ApplicationDbContext context, ResourceManager resourceManager)
    {
        _context = context;
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Берёт блокировку строки игрока (FOR UPDATE) до конца транзакции текущего запроса.
    /// </summary>
    /// <remarks>
    /// Транзакцию открывает конструктор <see cref="UnitOfWork"/> – он инжектится в этот менеджер
    /// именно затем, чтобы блокировка не выродилась в no-op в autocommit-режиме,
    /// когда никто выше по графу зависимостей UnitOfWork ещё не создал.
    /// </remarks>
    public void LockDbPlayerRow(int playerId)
    {
        _context.Database.ExecuteSqlRaw("SELECT 1 FROM \"Players\" WHERE \"Id\" = {0} FOR UPDATE", playerId);
    }

    public void WriteOffResources(int playerId, Resource[] resources)
    {
        resources = resources.Where(x => x.Value > 0).ToArray();
        var dbResources = _context.Resources.Where(x => x.PlayerId == playerId).ToArray();
        var resourceTypes = _resourceManager.GetResourceTypes();
        foreach (var group in resources.GroupBy(x => x.Type.Id))
        {
            var dbResource = dbResources.FirstOrDefault(x => x.TypeId == group.Key);
            if (dbResource == null || dbResource.Value < group.Sum(x => x.Value))
            {
                throw new BusinessException("Недостаточно " + GetResourceName(group.First(), resourceTypes));
            }
        }

        foreach (var needResource in resources)
        {
            var dbResource = dbResources.First(x => x.TypeId == needResource.Type.Id);
            dbResource.Value -= needResource.Value;
        }
    }

    public void GrantReputation(int playerId, int neighborId, int points)
    {
        var reputation = _context.NeighborReputations.Local.FirstOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId)
                         ?? _context.NeighborReputations.FirstOrDefault(x => x.PlayerId == playerId && x.NeighborId == neighborId);

        if (reputation == null)
        {
            reputation = new()
                { PlayerId = playerId, NeighborId = neighborId };

            _context.NeighborReputations.Add(reputation);
        }

        reputation.Points += points;
    }

    public void GrantResource(int playerId, int typeId, int value)
    {
        if (value == 0)
        {
            return;
        }

        var dbResource = _context.Resources.Local.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId)
                         ?? _context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);

        if (dbResource == null)
        {
            dbResource = new()
                { PlayerId = playerId, TypeId = typeId };

            _context.Resources.Add(dbResource);
        }

        dbResource.Value += value;
    }

    private string GetResourceName(Resource resource, ResourceType[] resourceTypes)
    {
        return resource.Type.Name ?? resourceTypes.First(x => x.Id == resource.Type.Id).Name ?? "ресурса";
    }
}
