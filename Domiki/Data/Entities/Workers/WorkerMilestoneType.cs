namespace Domiki.Web.Data.Entities;

/// <summary>
/// Вид пожизненной вехи трудяги.
/// </summary>
public enum WorkerMilestoneType
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Суммарное число использований навыков трудяги достигло <c>1</c>.
    /// </summary>
    FirstShift = 1,

    /// <summary>
    /// Суммарное число использований навыков трудяги достигло <c>100</c>.
    /// </summary>
    HundredthShift = 2,

    /// <summary>
    /// Число использований навыка трудяги в одном типе домика достигло <c>50</c>.
    /// </summary>
    SkilledHand = 3,

    /// <summary>
    /// С момента найма трудяги прошло <c>30</c> суток.
    /// </summary>
    MonthInBarracks = 4,

    /// <summary>
    /// У трудяги и другого трудяги игрока есть по <c>25</c> использований в одном типе домика.
    /// </summary>
    TwoAtBench = 5,

    /// <summary>
    /// Пожизненное число завершённых походов трудяги достигло <c>10</c>.
    /// </summary>
    TenthRoad = 6,
}
