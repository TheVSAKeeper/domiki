using Domiki.Web.Activities.Models;

namespace Domiki.Web.Village.Models;

/// <summary>
/// Каталог деревень для экрана «Мир» – рейтинг обжитости и текущий сезон номинаций.
/// </summary>
/// <remarks>
/// Собирается в <see cref="WorldManager.GetWorld"/> и отдаётся на клиент как <see cref="Dto.WorldDto"/>.
/// </remarks>
public class World
{
    /// <summary>
    /// Все видимые деревни (реальные игроки и NPC-соседи).
    /// </summary>
    /// <remarks>
    /// Отсортированы по убыванию <see cref="WorldVillage.Level"/>.
    /// </remarks>
    public WorldVillage[] Villages { get; set; } = [];

    /// <summary>
    /// Текущий сезонный период рейтингов.
    /// </summary>
    public required Season Season { get; set; }

    /// <summary>
    /// Летопись последних завершённых толок.
    /// </summary>
    /// <remarks>
    /// Наполняется в <see cref="Activities.TolokaManager.GetArtifacts"/>, не более
    /// <see cref="Activities.TolokaManager.TolokaArtifactShowCount"/> штук, свежие первыми.
    /// </remarks>
    public TolokaArtifact[] TolokaArtifacts { get; set; } = [];
}

/// <summary>
/// Одна деревня в каталоге экрана «Мир» – реальный игрок или декоративный NPC-сосед.
/// </summary>
public class WorldVillage
{
    /// <summary>
    /// Идентификатор игрока-владельца.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – деревня NPC (см. <see cref="IsNpc"/>), не привязана к игроку.
    /// </remarks>
    public int? PlayerId { get; set; }

    /// <summary>
    /// Название деревни.
    /// </summary>
    public string? VillageName { get; set; }

    /// <summary>
    /// Индекс пиктограммы герба.
    /// </summary>
    public int CrestIcon { get; set; }

    /// <summary>
    /// Индекс цвета герба.
    /// </summary>
    public int CrestColor { get; set; }

    /// <summary>
    /// Обжитость деревни, по которой отсортирован каталог.
    /// </summary>
    /// <remarks>
    /// См. <see cref="VillageLevel.Level"/>.
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Является ли деревня NPC-соседом.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – NPC-сосед с фиксированным представлением, не реальный игрок.
    /// </remarks>
    public bool IsNpc { get; set; }

    /// <summary>
    /// Принадлежит ли деревня текущему игроку.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> – это деревня текущего игрока.
    /// </remarks>
    public bool IsMe { get; set; }

    /// <summary>
    /// Ресурс, которым торгует NPC-сосед – ссылка на <see cref="Reference.Models.ResourceType.Id"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public int? NpcResourceTypeId { get; set; }

    /// <summary>
    /// Технический код NPC-соседа.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> для деревень игроков (см. <see cref="IsNpc"/>).
    /// </remarks>
    public string? NpcLogicName { get; set; }

    /// <summary>
    /// Число выполненных заказов за текущий сезон – счётчик номинации «Лучший поставщик».
    /// </summary>
    public int SeasonOrders { get; set; }

    /// <summary>
    /// Вклад в толоку за текущий сезон – счётчик номинации «Герой толоки».
    /// </summary>
    public int SeasonToloka { get; set; }

    /// <summary>
    /// Число завершённых экспедиций за текущий сезон – счётчик номинации «Дальние странники».
    /// </summary>
    public int SeasonExpeditions { get; set; }

    /// <summary>
    /// Очки уюта деревни – основа номинации «Самая уютная деревня».
    /// </summary>
    /// <remarks>
    /// Совпадает с <see cref="VillageLevel.Comfort"/>.
    /// </remarks>
    public int Comfort { get; set; }
}

/// <summary>
/// Снимок чужой деревни при read-only визите игрока из экрана «Мир».
/// </summary>
/// <remarks>
/// Собирается в <see cref="WorldManager.VisitVillage"/> и отдаётся на клиент как <see cref="Dto.VillageVisitDto"/>.
/// </remarks>
public class VillageVisit
{
    /// <summary>
    /// Название посещаемой деревни.
    /// </summary>
    public required string VillageName { get; set; }

    /// <summary>
    /// Индекс пиктограммы герба.
    /// </summary>
    public int CrestIcon { get; set; }

    /// <summary>
    /// Индекс цвета герба.
    /// </summary>
    public int CrestColor { get; set; }

    /// <summary>
    /// Обжитость посещаемой деревни и её слагаемые.
    /// </summary>
    public required VillageLevel Level { get; set; }

    /// <summary>
    /// Постройки посещаемой деревни с их уровнями.
    /// </summary>
    public VisitBuilding[] Buildings { get; set; } = [];
}

/// <summary>
/// Одна постройка в списке визита в чужую деревню.
/// </summary>
public class VisitBuilding
{
    /// <summary>
    /// Название типа постройки.
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Уровень постройки.
    /// </summary>
    public int Level { get; set; }
}
