using Domiki.Web.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Business.Core
{
    public class WorkerManager
    {
        private const int PlodderModificatorId = 1;

        private static readonly string[] WorkerNames =
        {
            "Аким",
            "Борис",
            "Варвара",
            "Глеб",
            "Дарья",
            "Егор",
            "Ждан",
            "Злата",
            "Илья",
            "Кира",
            "Лада",
            "Мирон",
            "Нина",
            "Остап",
            "Пелагея",
            "Роман",
            "Сава",
            "Тая",
            "Ульяна",
            "Фёдор",
            "Ярина",
        };

        private readonly Data.ApplicationDbContext _context;
        private readonly ResourceManager _resourceManager;
        private readonly PlayerResourceManager _playerResourceManager;

        public WorkerManager(Data.ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager)
        {
            _context = context;
            _resourceManager = resourceManager;
            _playerResourceManager = playerResourceManager;
        }

        public IEnumerable<Worker> GetWorkers(int playerId)
        {
            var capacity = GetCapacity(playerId);
            var currentCount = _context.Workers.Count(x => x.PlayerId == playerId);
            if (currentCount < capacity)
            {
                _playerResourceManager.LockDbPlayerRow(playerId);
                EnsureWorkers(playerId);
                _context.SaveChanges();
            }

            return MapWorkers(playerId);
        }

        public Data.Worker[] EnsureWorkers(int playerId)
        {
            var capacity = GetCapacity(playerId);
            var currentWorkers = _context.Workers.Where(x => x.PlayerId == playerId).OrderBy(x => x.Id).ToArray();
            var traits = _resourceManager.GetTraits();
            while (currentWorkers.Length < capacity)
            {
                var worker = new Data.Worker
                {
                    PlayerId = playerId,
                    Name = WorkerNames[Random.Shared.Next(WorkerNames.Length)],
                    TraitId = traits[Random.Shared.Next(traits.Length)].Id,
                };
                _context.Workers.Add(worker);
                currentWorkers = currentWorkers.Append(worker).ToArray();
            }

            _context.SaveChanges();
            ReconcileManufactures(playerId, currentWorkers);
            return currentWorkers;
        }

        public int GetCapacity(int playerId)
        {
            var domikTypes = _resourceManager.GetDomikTypes();
            var capacity = 0;
            foreach (var domik in _context.Domiks.Where(x => x.PlayerId == playerId).ToArray())
            {
                if (domik.Level == 0)
                {
                    continue;
                }

                var domikType = domikTypes.First(x => x.Id == domik.TypeId);
                var level = domikType.Levels.First(x => x.Value == domik.Level);
                capacity += level.Modificators.FirstOrDefault(x => x.Type.Id == PlodderModificatorId)?.Value ?? 0;
            }

            return capacity;
        }

        private void ReconcileManufactures(int playerId, Data.Worker[] currentWorkers)
        {
            var manufactures = _context.Manufactures.Where(x => x.DomikPlayerId == playerId).OrderBy(x => x.Id).ToArray();
            foreach (var manufacture in manufactures)
            {
                var assigned = currentWorkers.Where(x => x.ManufactureId == manufacture.Id).OrderBy(x => x.Id).ToArray();
                var missing = manufacture.PlodderCount - assigned.Length;
                if (missing <= 0)
                {
                    continue;
                }

                var freeWorkers = currentWorkers.Where(x => x.ManufactureId == null).OrderBy(x => x.Id).Take(missing).ToArray();
                foreach (var worker in freeWorkers)
                {
                    worker.ManufactureId = manufacture.Id;
                }
            }
        }

        private IEnumerable<Worker> MapWorkers(int playerId)
        {
            var traits = _resourceManager.GetTraits().ToDictionary(x => x.Id, x => x);
            var workers = _context.Workers.Where(x => x.PlayerId == playerId).OrderBy(x => x.Id).ToArray();
            var workerIds = workers.Select(x => x.Id).ToArray();
            var skills = _context.WorkerSkills.Where(x => workerIds.Contains(x.WorkerId)).ToArray()
                .GroupBy(x => x.WorkerId)
                .ToDictionary(x => x.Key, x => x.OrderBy(y => y.DomikTypeId).Select(y => new WorkerSkill
                {
                    DomikTypeId = y.DomikTypeId,
                    Uses = y.Uses,
                    BonusPercent = WorkerSkillCalculator.GetBonusPercent(y.Uses),
                }).ToArray());

            return workers.Select(x => new Worker
            {
                Id = x.Id,
                Name = x.Name,
                Trait = traits[x.TraitId],
                ManufactureId = x.ManufactureId,
                Skills = skills.GetValueOrDefault(x.Id, Array.Empty<WorkerSkill>()),
            }).ToArray();
        }
    }
}
