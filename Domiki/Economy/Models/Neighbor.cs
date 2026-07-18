namespace Domiki.Web.Economy.Models;

/// <summary>
/// Справочник соседей деревни – кому игрок выполняет заказы и с кем растёт репутация.
/// </summary>
/// <remarks>
/// Собирается в <see cref="Reference.ResourceManager.GetNeighbors"/>.
/// </remarks>
public class Neighbor
{
    /// <summary>
    /// Идентификатор соседа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое имя соседа.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Технический код соседа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Тип ресурса, который сосед просит в заказах в первую очередь – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int PrimaryResourceTypeId { get; set; }

    /// <summary>
    /// Дополнительный тип ресурса, который сосед может просить в заказах – ссылка на справочник <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – у соседа только один вид запроса.
    /// </remarks>
    public int? SecondaryResourceTypeId { get; set; }

    /// <summary>
    /// Обжитость деревни, с которой сосед появляется на доске заказов игрока.
    /// </summary>
    public int UnlockLevel { get; set; }
}
