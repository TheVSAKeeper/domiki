using Domiki.Web.Core;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure.Dto;
using Domiki.Web.Village;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Постраничная выдача журнала событий вглубь.
/// </summary>
/// <remarks>
/// Первая страница приезжает вместе со снимком состояния (<c>GetGameState</c>) и здесь не дублируется – так офлайн-снимок
/// и его контрактный тест остаются нетронутыми. Этот контроллер отдаёт то, что старше уже показанного, и то же самое
/// с начала при выборе фильтра.
/// </remarks>
public class JournalController : GameControllerBase
{
    /// <summary>
    /// Окно ретроспективы в шапке журнала.
    /// </summary>
    public static readonly TimeSpan DigestWindow = TimeSpan.FromDays(7);


    private readonly PlayerEventManager _playerEventManager;
    private readonly RelocationManager _relocationManager;

    public JournalController(DomikManager domikManager, PlayerEventManager playerEventManager, RelocationManager relocationManager)
        : base(domikManager)
    {
        _playerEventManager = playerEventManager;
        _relocationManager = relocationManager;
    }

    /// <summary>
    /// Отдаёт страницу журнала, новейшие записи первыми.
    /// </summary>
    /// <param name="beforeId">Идентификатор последней показанной записи; отдаются строго старшие. <c>0</c> – с начала.</param>
    /// <param name="count">Размер страницы; больше <see cref="PlayerEventManager.MaxPageSize"/> не отдаётся.</param>
    /// <param name="group">Группа для фильтра; <see cref="PlayerEventGroup.None"/> – без фильтра.</param>
    [HttpGet]
    [Route("/Domiki/GetJournalPage")]
    public JournalPageDto GetJournalPage(long beforeId = 0, int count = 30, PlayerEventGroup group = PlayerEventGroup.None)
    {
        if (beforeId < 0)
        {
            throw new BusinessException("Неверный курсор журнала");
        }

        if (count <= 0 || count > PlayerEventManager.MaxPageSize)
        {
            throw new BusinessException($"Размер страницы журнала – от 1 до {PlayerEventManager.MaxPageSize}");
        }

        if (!Enum.IsDefined(group))
        {
            throw new BusinessException("Неизвестная группа событий");
        }

        var playerId = GetPlayerId();
        var isFirstPage = beforeId == 0;

        return new()
        {
            Events = _playerEventManager.GetEventsBefore(playerId, isFirstPage ? null : beforeId, count, group).Select(x => x.ToDto()).ToArray(),
            TotalCount = _playerEventManager.CountEvents(playerId, group),
            VillageRuns = isFirstPage
                ? _relocationManager.GetVillageRuns(playerId)
                    .Select(x => new VillageRunDto
                    {
                        VillageName = x.VillageName,
                        StartDate = DateTimeHelper.AsUtc(x.StartDate),
                        EndDate = x.EndDate == null ? null : DateTimeHelper.AsUtc(x.EndDate.Value),
                    })
                    .ToArray()
                : [],
            Digest = isFirstPage
                ? _playerEventManager.CountByGroup(playerId, DateTimeHelper.GetNowDate() - DigestWindow)
                    .OrderByDescending(x => x.Value)
                    .Select(x => new JournalDigestEntryDto { Group = x.Key.ToString(), Count = x.Value })
                    .ToArray()
                : [],
        };
    }
}
