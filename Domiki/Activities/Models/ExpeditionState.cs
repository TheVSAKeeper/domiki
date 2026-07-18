namespace Domiki.Web.Activities.Models;

/// <summary>
/// Состояние экспедиций игрока.
/// </summary>
/// <remarks>
/// Собирается в <see cref="ExpeditionManager.GetExpeditions"/> и отдаётся на клиент как <see cref="Dto.ExpeditionStateDto"/>.
/// </remarks>
public class ExpeditionState
{
    /// <summary>
    /// Отряды, находящиеся в походе прямо сейчас.
    /// </summary>
    public Expedition[] Active { get; set; } = [];

    /// <summary>
    /// Справочник доступных типов экспедиций.
    /// </summary>
    public ExpeditionType[] Types { get; set; } = [];

    /// <summary>
    /// Число завершённых экспедиций подряд без редкой добычи – счётчик гарантии (<c>pity</c>).
    /// </summary>
    /// <remarks>
    /// Сравнивается с <see cref="PityThreshold"/>; сбрасывается в <c>0</c> при выпадении редкой добычи
    /// (см. <see cref="ExpeditionManager.FinishExpedition"/>).
    /// </remarks>
    public int ExpeditionsSincePity { get; set; }

    /// <summary>
    /// Порог <see cref="ExpeditionsSincePity"/>, при достижении которого следующая добыча гарантированно редкая.
    /// </summary>
    /// <remarks>
    /// Равен <see cref="ExpeditionManager.ExpeditionPityThreshold"/>.
    /// </remarks>
    public int PityThreshold { get; set; }

    /// <summary>
    /// Максимум одновременно идущих экспедиций.
    /// </summary>
    /// <remarks>
    /// Равен уровню разведывательной хижины (см. <see cref="ExpeditionManager.GetScoutHutLevel"/>).
    /// </remarks>
    public int MaxActive { get; set; }
}
