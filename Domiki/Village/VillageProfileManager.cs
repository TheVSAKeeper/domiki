using Domiki.Web.Data;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Village;

/// <summary>
/// Уклад деревни – специализация под одного из соседей, ускоряющая производство в двух постройках его профиля.
/// </summary>
/// <remarks>
/// Канон – GAMEDESIGN.md §3 Слой 4 «Уклад деревни»: приобретение и смена гейтятся обжитостью, репутацией у выбранного
/// соседа и кулдауном между сменами; сам множитель применяется в <see cref="Core.DomikManager.StartManufacture"/>
/// по справочнику <see cref="Reference.ResourceManager.GetVillageProfileEffects"/>.
/// </remarks>
public class VillageProfileManager
{
    /// <summary>
    /// Обжитость деревни, необходимая для принятия или смены уклада.
    /// </summary>
    public const int VillageLevelRequirement = 20;

    /// <summary>
    /// Репутация у выбранного соседа, необходимая для принятия или смены уклада.
    /// </summary>
    public const int ReputationRequirement = 10;

    /// <summary>
    /// Сколько суток должно пройти между сменами уклада.
    /// </summary>
    public const int ProfileChangeCooldownDays = 7;

    private readonly ApplicationDbContext _context;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly ResourceManager _resourceManager;

    public VillageProfileManager(ApplicationDbContext context, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator, ResourceManager resourceManager)
    {
        _context = context;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Принимает или сменяет уклад деревни на профиль указанного соседа.
    /// </summary>
    /// <param name="playerId">Игрок.</param>
    /// <param name="neighborId">Сосед, чей уклад принимается – ссылка на справочник <see cref="Data.Entities.Neighbor.Id"/>.</param>
    public void SetVillageProfile(int playerId, int neighborId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);

        if (!_resourceManager.GetVillageProfileEffects().Any(x => x.NeighborId == neighborId))
        {
            throw new BusinessException("У этого соседа нет своего уклада");
        }

        if (_villageLevelCalculator.GetLevel(playerId).Level < VillageLevelRequirement)
        {
            throw new BusinessException($"Уклад деревни откроется на обжитости {VillageLevelRequirement}");
        }

        var reputation = _context.NeighborReputations.AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.NeighborId == neighborId)
            .Select(x => (int?)x.Points)
            .SingleOrDefault() ?? 0;

        if (reputation < ReputationRequirement)
        {
            throw new BusinessException($"Для уклада нужно доброе имя {ReputationRequirement} у этого соседа");
        }

        var current = _context.Players.AsNoTracking()
            .Where(x => x.Id == playerId)
            .Select(x => new { x.ProfileNeighborId, x.ProfileChangedDate })
            .Single();

        if (current.ProfileNeighborId == neighborId)
        {
            throw new BusinessException("Этот уклад уже принят");
        }

        var date = DateTimeHelper.GetNowDate();
        if (current.ProfileChangedDate is { } changedDate && date - changedDate < TimeSpan.FromDays(ProfileChangeCooldownDays))
        {
            throw new BusinessException($"Сменить уклад можно не чаще раза в {ProfileChangeCooldownDays} суток");
        }

        var dbPlayer = _context.Players.Single(x => x.Id == playerId);
        dbPlayer.ProfileNeighborId = neighborId;
        dbPlayer.ProfileChangedDate = date;
    }
}
