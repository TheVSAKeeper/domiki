using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Игровой профиль игрока – корневая сущность игры.
/// </summary>
/// <remarks>
/// Деревня, счётчики механик (pity, суточный кап золота, гостинцы соседей), привязка к внешнему аккаунту.
/// </remarks>
[Index(nameof(AspNetUserId), IsUnique = true)]
public class Player
{
    /// <summary>
    /// Идентификатор игрока.
    /// </summary>
    /// <remarks>
    /// <see cref="System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity"/> намеренно не используется (см. закомментированный атрибут) – игрок создаётся лениво в <see cref="Core.DomikManager.GetPlayerId"/>.
    /// </remarks>
    [Key]
    //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Имя игрока.
    /// </summary>
    [MaxLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Name { get; set; }

    /// <summary>
    /// Название деревни игрока, задаётся отдельно от имени игрока (см. <see cref="Core.DomikManager.SetVillageIdentity"/>).
    /// </summary>
    [MaxLength(100)]
    public string? VillageName { get; set; }

    /// <summary>
    /// Индекс выбранной игроком иконки герба деревни.
    /// </summary>
    public int CrestIcon { get; set; }

    /// <summary>
    /// Индекс выбранного игроком цвета герба деревни.
    /// </summary>
    public int CrestColor { get; set; }

    /// <summary>
    /// Счётчик экспедиций без редкой находки.
    /// </summary>
    /// <remarks>
    /// При достижении <see cref="Activities.ExpeditionManager.ExpeditionPityThreshold"/> следующая находка гарантированно редкая, сбрасывается в <c>0</c> (pity-механика §8.6).
    /// </remarks>
    public int ExpeditionsSincePity { get; set; }

    /// <summary>
    /// Момент последнего происшествия с пропавшим в походе трудягой.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Ограничивает частоту завязок кулдауном <see cref="Activities.IncidentManager.IncidentCooldownHours"/>.
    /// </remarks>
    public DateTime? LastIncidentDate { get; set; }

    /// <summary>
    /// Момент последнего происшествия-загадки в постройке.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Ограничивает частоту завязок кулдауном <see cref="Activities.IncidentManager.DomikIncidentCooldownHours"/>.
    /// </remarks>
    public DateTime? LastDomikIncidentDate { get; set; }

    /// <summary>
    /// Момент последней выданной вехи трудяги.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Ограничивает частоту выдачи кулдауном <see cref="Workers.WorkerMilestoneManager.WorkerMilestoneCooldownHours"/>.
    /// </remarks>
    public DateTime? LastWorkerMilestoneDate { get; set; }

    /// <summary>
    /// Счётчик визитов-возвратов подряд без крупного гостинца от соседей.
    /// </summary>
    /// <remarks>
    /// Каждый седьмой гостинец крупный (декор вместо ресурсов), счётчик сбрасывается в <c>0</c> (см. <see cref="Economy.GiftManager"/> – полоса без обнуления при пропуске).
    /// </remarks>
    public int VisitsSinceBigGift { get; set; }

    /// <summary>
    /// Момент последнего взятия витрины «Пока вас не было» (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – игрок ещё ни разу её не открывал (новый игрок), гостинец соседа не выдаётся до первого визита.
    /// </remarks>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Момент, с которого доска заказов игрока снова может пополняться.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – пополнение не отложено (см. <see cref="Economy.OrderManager.EnsureOrderBoard"/>).
    /// </remarks>
    public DateTime? NextOrderRefillAt { get; set; }

    /// <summary>
    /// Сосед, с которым деревня нынче водит дружбу.
    /// </summary>
    /// <remarks>
    /// Ссылка на справочник соседей (см. <see cref="Reference.ResourceManager.GetNeighbors"/>): заказы дружественного соседа
    /// чаще появляются на доске (см. <see cref="Economy.OrderManager.FriendWeight"/>). <see langword="null"/> – игрок ни с кем не дружит.
    /// </remarks>
    public int? FriendNeighborId { get; set; }

    /// <summary>
    /// Сколько золота уже добыто прямой добычей (рудником) за текущие календарные сутки UTC.
    /// </summary>
    /// <value>Золото.</value>
    /// <remarks>
    /// Вместе с <see cref="GoldMinedDate"/> реализует суточный кап золота по уровню рудника.
    /// </remarks>
    public int GoldMinedToday { get; set; }

    /// <summary>
    /// Календарная дата (UTC), за которую считается <see cref="GoldMinedToday"/>.
    /// </summary>
    /// <remarks>
    /// При смене даты счётчик сбрасывается в <c>0</c>.
    /// </remarks>
    public DateTime? GoldMinedDate { get; set; }

    /// <summary>
    /// Момент последнего «подсобить», совершённого этим игроком как гостем в чужой деревне.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// Кап – одно «подсобить» в UTC-день (см. <see cref="Village.HelpManager.Help"/>).
    /// </remarks>
    public DateTime? LastHelpDate { get; set; }

    /// <summary>
    /// Сколько раз деревне этого игрока подсобили за текущие календарные сутки UTC.
    /// </summary>
    /// <remarks>
    /// Вместе с <see cref="HelpsReceivedDate"/> реализует суточный кап <see cref="Village.HelpManager.HostHelpCapPerDay"/> на число визитов «подсобить».
    /// </remarks>
    public int HelpsReceivedToday { get; set; }

    /// <summary>
    /// Календарная дата (UTC), за которую считается <see cref="HelpsReceivedToday"/>.
    /// </summary>
    /// <remarks>
    /// При смене даты счётчик логически сбрасывается в <c>0</c>.
    /// </remarks>
    public DateTime? HelpsReceivedDate { get; set; }

    /// <summary>
    /// Игрок включил автоматическое кормление уставших трудяг хлебом.
    /// </summary>
    /// <remarks>
    /// Наполовину сокращает срок отдыха при наличии хлеба (см. <see cref="Core.DomikManager.FinishManufacture"/>).
    /// </remarks>
    public bool FeedWorkers { get; set; }

    /// <summary>
    /// Остаток зарядов «нетронутых залежей» – ограниченное число ускоренных производств.
    /// </summary>
    /// <value><c>×4</c> на первых зарядах, затем <c>×2</c>.</value>
    /// <remarks>
    /// Тратится в <see cref="Core.DomikManager.StartManufacture"/> на коротких рецептах вне Лавки; не истекает и не обнуляется,
    /// значение по умолчанию <c>24</c> выдано и существующим игрокам миграцией.
    /// </remarks>
    public int ZealCharges { get; set; } = 24;

    /// <summary>
    /// Идентификатор внешнего аккаунта ASP.NET Identity (claim <c>NameIdentifier</c>), к которому привязан игрок.
    /// </summary>
    [MaxLength(450)]
    [Required(AllowEmptyStrings = false)]
    public required string AspNetUserId { get; set; }

    /// <summary>
    /// Служебное поле пессимистичной блокировки – не игровые данные.
    /// </summary>
    /// <remarks>
    /// Перезаписывается новым Guid при любой операции игрока, чтобы форсировать UPDATE строки и сериализовать конкурентные операции
    /// одного игрока (см. <see cref="Infrastructure.PlayerResourceManager.LockDbPlayerRow"/>).
    /// </remarks>
    [ConcurrencyCheck]
    public Guid Version { get; set; }

    /// <summary>
    /// Ресурсы, накопленные игроком.
    /// </summary>
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
