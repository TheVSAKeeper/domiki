using Domiki.Web.Data.Entities;

namespace Domiki.Web.Activities.Models;

/// <summary>
/// Справочный тип толоки – собираемый ресурс, цель и баффы производству по завершении.
/// </summary>
/// <remarks>
/// Загружается из БД целиком (см. <see cref="Reference.ResourceManager.GetTolokaTypes"/>).
/// </remarks>
public class TolokaType
{
    /// <summary>
    /// Идентификатор типа толоки.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название толоки.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя типа, используется как ключ на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Ресурс, который собирают участники, – ссылка на <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    public int ResourceTypeId { get; set; }

    /// <summary>
    /// Базовое целевое значение общего счётчика для одной толоки этого типа.
    /// </summary>
    /// <remarks>
    /// При старте новой толоки масштабируется числом участников предыдущей (см. <see cref="TolokaManager.Contribute"/>).
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Вес типа при случайном выборе следующей толоки.
    /// </summary>
    /// <remarks>
    /// Чем больше, тем чаще выпадает относительно других типов (см. <see cref="TolokaManager.PickTolokaType"/>).
    /// </remarks>
    public int RotationWeight { get; set; }

    /// <summary>
    /// Баффы производству, которые даёт завершение толоки этого типа – по одному на тип домика.
    /// </summary>
    public TolokaTypeEffect[] Effects { get; set; } = [];
}
