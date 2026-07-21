namespace Domiki.Web.Economy.Models;

/// <summary>
/// Одна позиция ассортимента обоза – тип ресурса и цена покупки одной единицы.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.ConvoyManager.GetConvoys"/> и отдаётся на клиент как <see cref="Dto.ConvoyItemDto"/>.
/// </remarks>
public class ConvoyItem
{
    /// <summary>
    /// Тип покупаемого ресурса – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Цена одной единицы ресурса в монетах.
    /// </summary>
    /// <remarks>
    /// Считается как <see cref="Reference.ResourceManager.GetMarketValue"/> × <see cref="Economy.ConvoyManager.PriceMultiplier"/>.
    /// </remarks>
    public int Price { get; set; }
}

/// <summary>
/// Обоз одного соседа – ассортимент, цены и остаток скользящего суточного лимита покупок.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Economy.ConvoyManager.GetConvoys"/> и отдаётся на клиент как <see cref="Dto.ConvoyDto"/>.
/// </remarks>
public class Convoy
{
    /// <summary>
    /// Сосед, чей обоз описывается.
    /// </summary>
    public required Neighbor Neighbor { get; set; }

    /// <summary>
    /// Позиции ассортимента, доступные для покупки прямо сейчас.
    /// </summary>
    /// <remarks>
    /// Пусто, если <see cref="IsLocked"/> – в этом случае показывать ассортимент нет смысла, обоз недоступен для покупки.
    /// </remarks>
    public ConvoyItem[] Items { get; set; } = [];

    /// <summary>
    /// Суточный лимит покупок у соседа – <see cref="Economy.ConvoyManager.BaseLimit"/> или <see cref="Economy.ConvoyManager.HighLimit"/>
    /// в зависимости от репутации.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Сколько ещё единиц ресурса можно купить у соседа в текущем окне.
    /// </summary>
    public int Remaining { get; set; }

    /// <summary>
    /// Момент, когда скользящее окно лимита обновится и остаток снова станет полным.
    /// </summary>
    /// <value>Момент в UTC.</value>
    /// <remarks>
    /// <see langword="null"/>, если окно ещё не начато – в текущем окне не было ни одной покупки.
    /// </remarks>
    public DateTime? WindowResetDate { get; set; }

    /// <summary>
    /// Признак того, что обоз соседа закрыт для покупок.
    /// </summary>
    /// <remarks>
    /// Единственная причина – репутации у соседа меньше <see cref="Economy.ConvoyManager.AccessReputationThreshold"/>.
    /// </remarks>
    public bool IsLocked { get; set; }
}
