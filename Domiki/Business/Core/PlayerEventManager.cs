using Domiki.Web.Business.Models;
using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Domiki.Web.Business.Core
{
    public class PlayerEventManager
    {
        private readonly ApplicationDbContext _context;

        public PlayerEventManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Record(int playerId, PlayerEventType type, object payload)
        {
            _context.PlayerEvents.Add(new PlayerEvent
            {
                PlayerId = playerId,
                Type = type,
                Date = DateTimeHelper.GetNowDate(),
                Data = JsonSerializer.Serialize(payload),
            });
        }

        public RecapModel TakeRecap(int playerId, DateTime now)
        {
            var events = _context.PlayerEvents.AsNoTracking().Where(x => x.PlayerId == playerId).OrderBy(x => x.Date).Take(500).ToList();
            var lastSeen = _context.Players.AsNoTracking().Where(x => x.Id == playerId).Select(x => x.LastSeen).FirstOrDefault();
            if (events.Count > 0)
            {
                var ids = events.Select(x => x.Id).ToList();
                _context.PlayerEvents.Where(x => ids.Contains(x.Id)).ExecuteDelete();
            }

            _context.Database.ExecuteSqlRaw("UPDATE \"Players\" SET \"LastSeen\" = {0} WHERE \"Id\" = {1}", now, playerId);

            return new RecapModel
            {
                AwaySeconds = lastSeen == null ? 0 : Math.Max(0, (int)(now - lastSeen.Value).TotalSeconds),
                Events = events.Select(x => new RecapEventModel
                {
                    Type = x.Type,
                    Date = x.Date,
                    Data = JsonSerializer.Deserialize<JsonElement>(x.Data),
                }).ToList(),
            };
        }
    }
}
