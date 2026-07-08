using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class TolokaManager
    {
        public const int TolokaBuffPercent = 25;
        public const int TolokaBuffSeconds = 8 * 3600;
        public const int TolokaUnlockLevel = 20;

        private readonly Data.ApplicationDbContext _context;
        private readonly ResourceManager _resourceManager;
        private readonly PlayerResourceManager _playerResourceManager;
        private readonly VillageLevelCalculator _villageLevelCalculator;

        public TolokaManager(Data.UnitOfWork uow, Data.ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator)
        {
            _context = context;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
            _villageLevelCalculator = villageLevelCalculator;
        }

        public TolokaState GetToloka(DateTime date, int playerId)
        {
            var tolokaTypes = _resourceManager.GetTolokaTypes();
            var dbToloka = _context.Tolokas.Single(x => x.CompletedDate == null);
            var contribution = _context.TolokaContributions
                .Where(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId)
                .Select(x => x.Value)
                .SingleOrDefault();
            var buffUntil = GetBuffUntil(playerId, date);

            return new TolokaState
            {
                Active = ToModel(dbToloka, tolokaTypes),
                MyContribution = contribution,
                CanContribute = _villageLevelCalculator.GetLevel(playerId).Level >= TolokaUnlockLevel,
                UnlockLevel = TolokaUnlockLevel,
                BuffActive = buffUntil != null,
                BuffUntil = buffUntil,
                BuffPercent = TolokaBuffPercent,
            };
        }

        public void Contribute(int playerId, int amount, DateTime date)
        {
            if (amount <= 0)
            {
                throw new BusinessException("Неверное количество");
            }

            _playerResourceManager.LockDbPlayerRow(playerId);

            if (_villageLevelCalculator.GetLevel(playerId).Level < TolokaUnlockLevel)
            {
                throw new BusinessException($"Толока откроется при обжитости {TolokaUnlockLevel}");
            }

            var dbToloka = LockActiveToloka();
            var tolokaTypes = _resourceManager.GetTolokaTypes();
            var tolokaType = tolokaTypes.First(x => x.Id == dbToloka.TolokaTypeId);

            _playerResourceManager.WriteOffResources(playerId, new[]
            {
                new Resource { Type = new ResourceType { Id = tolokaType.ResourceTypeId }, Value = amount },
            });

            var contribution = _context.TolokaContributions.Local.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId)
                ?? _context.TolokaContributions.FirstOrDefault(x => x.TolokaId == dbToloka.Id && x.PlayerId == playerId);
            if (contribution == null)
            {
                contribution = new Data.TolokaContribution { TolokaId = dbToloka.Id, PlayerId = playerId };
                _context.TolokaContributions.Add(contribution);
            }

            contribution.Value += amount;
            dbToloka.Collected += amount;

            if (dbToloka.Collected >= tolokaType.Goal)
            {
                dbToloka.CompletedDate = date;
                _context.SaveChanges();
                _context.Tolokas.Add(new Data.Toloka
                {
                    TolokaTypeId = PickTolokaType(tolokaTypes).Id,
                    Collected = 0,
                    StartDate = date,
                    CompletedDate = null,
                });
            }

            _context.SaveChanges();
        }

        public bool HasActiveBuff(int playerId, DateTime date)
        {
            return GetBuffUntil(playerId, date) != null;
        }

        private Data.Toloka LockActiveToloka()
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var tolokaId = _context.Tolokas.Where(x => x.CompletedDate == null).Select(x => x.Id).Single();
                _context.Database.ExecuteSqlRaw(@"SELECT 1 FROM ""Tolokas"" WHERE ""Id"" = {0} FOR UPDATE", tolokaId);
                var toloka = _context.Tolokas.Single(x => x.Id == tolokaId);
                if (toloka.CompletedDate == null)
                {
                    return toloka;
                }
            }

            throw new BusinessException("Толока обновляется, повторите");
        }

        private DateTime? GetBuffUntil(int playerId, DateTime date)
        {
            return _context.TolokaContributions
                .Where(x => x.PlayerId == playerId
                    && x.Toloka.CompletedDate != null
                    && x.Toloka.CompletedDate > date.AddSeconds(-TolokaBuffSeconds))
                .Select(x => (DateTime?)x.Toloka.CompletedDate.Value.AddSeconds(TolokaBuffSeconds))
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        private static TolokaType PickTolokaType(TolokaType[] tolokaTypes)
        {
            var totalWeight = tolokaTypes.Sum(x => x.RotationWeight);
            var roll = Random.Shared.Next(totalWeight);
            var cumulative = 0;
            foreach (var tolokaType in tolokaTypes)
            {
                cumulative += tolokaType.RotationWeight;
                if (roll < cumulative)
                {
                    return tolokaType;
                }
            }

            return tolokaTypes[^1];
        }

        private static Toloka ToModel(Data.Toloka dbToloka, TolokaType[] tolokaTypes)
        {
            return new Toloka
            {
                Id = dbToloka.Id,
                TolokaType = tolokaTypes.First(x => x.Id == dbToloka.TolokaTypeId),
                Collected = dbToloka.Collected,
                StartDate = dbToloka.StartDate,
            };
        }
    }
}
