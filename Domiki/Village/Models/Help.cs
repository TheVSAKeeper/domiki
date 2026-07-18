namespace Domiki.Web.Village.Models;

/// <summary>
/// Результат успешного «подсобить» – какая работа сокращена и чем это обернулось гостю.
/// </summary>
/// <remarks>
/// Собирается в <see cref="HelpManager.Help"/> и отдаётся на клиент как <see cref="Dto.HelpResultDto"/>.
/// </remarks>
public class HelpResult
{
    /// <summary>
    /// Название типа постройки, где сокращена работа.
    /// </summary>
    /// <remarks>
    /// Для производства – имя типа домика, в котором оно идёт, не название рецепта.
    /// </remarks>
    public required string DomikTypeName { get; set; }

    /// <summary>
    /// На сколько секунд сокращён остаток работы.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// Равно <see cref="HelpManager.HelpReducePercent"/> процентам от остатка на момент визита.
    /// </remarks>
    public int ReducedSeconds { get; set; }

    /// <summary>
    /// Монеты, выданные гостю в благодарность.
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="HelpManager.HelpRewardCoins"/>.
    /// </remarks>
    public int RewardCoins { get; set; }
}

/// <summary>
/// Доступность «подсобить» глазами гостя при визите в чужую деревню.
/// </summary>
/// <remarks>
/// Собирается в <see cref="HelpManager.GetVisitHelp"/> и отдаётся на клиент как часть <see cref="Dto.VillageVisitDto"/>.
/// </remarks>
public class VisitHelp
{
    /// <summary>
    /// Может ли гость подсобить хозяину прямо сейчас.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/>, если гость и хозяин – один игрок, у гостя не названа деревня, обжитость гостя ниже
    /// <see cref="UnlockLevel"/>, гость уже подсобил кому-то сегодня (<see cref="AlreadyHelpedToday"/>), у хозяина
    /// исчерпан суточный кап (<see cref="HostCapReached"/>) или у деревни хозяина нет активных работ (<see cref="HasActiveWork"/>).
    /// </remarks>
    public bool CanHelp { get; set; }

    /// <summary>
    /// Подсобил ли гость сегодня уже какой-то деревне.
    /// </summary>
    public bool AlreadyHelpedToday { get; set; }

    /// <summary>
    /// Исчерпан ли суточный кап деревни хозяина на число визитов «подсобить».
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="HelpManager.HostHelpCapPerDay"/>.
    /// </remarks>
    public bool HostCapReached { get; set; }

    /// <summary>
    /// Есть ли у деревни хозяина хотя бы одна активная работа (улучшение домика или производство) с остатком больше нуля.
    /// </summary>
    public bool HasActiveWork { get; set; }

    /// <summary>
    /// Порог обжитости, открывающий «подсобить».
    /// </summary>
    /// <remarks>
    /// Значение константы <see cref="HelpManager.HelpUnlockLevel"/>; сравнивается с обжитостью самого гостя, не хозяина.
    /// </remarks>
    public int UnlockLevel { get; set; }
}
