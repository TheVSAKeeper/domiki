using Domiki.Web.Activities.Dto;
using Domiki.Web.Core.Dto;
using Domiki.Web.Economy.Dto;
using Domiki.Web.Reference.Dto;
using Domiki.Web.Village.Dto;

namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Справочный срез состояния игры для внутриигрового справочника.
/// </summary>
/// <remarks>
/// Ответ <see cref="Infrastructure.GameStateController.GetWikiState"/> ничего одноразового не потребляет: в отличие от
/// <see cref="GameStateDto"/> он не выдаёт витрину «Пока вас не было», не двигает курсор доставки событий,
/// не выдаёт подарки и вехи трудяг и не забирает вклад в толоку. Поэтому справочник не съедает у игрока
/// сводку о том, что случилось без него. Полностью чистым чтением запрос не является: как и всякий
/// игровой запрос, он заводит игрока при первом обращении (<see cref="Core.DomikManager.GetPlayerId"/>).
/// </remarks>
public sealed record WikiStateDto
{
    /// <summary>
    /// Справочник типов построек вместе с персонализацией под игрока.
    /// </summary>
    /// <remarks>
    /// Персонализация – доступное количество, привязанный чертёж и гейты открытия (см. <see cref="DomikTypeDto"/>).
    /// </remarks>
    public required DomikTypeDto[] DomikTypes { get; init; }

    /// <summary>
    /// Справочник типов ресурсов.
    /// </summary>
    public required ResourceTypeDto[] ResourceTypes { get; init; }

    /// <summary>
    /// Справочник рецептов производства.
    /// </summary>
    public required ReceiptDto[] Receipts { get; init; }

    /// <summary>
    /// Текущая погода и её влияние на производство.
    /// </summary>
    public required WeatherStateDto Weather { get; init; }

    /// <summary>
    /// Декор игрока и накопленный уют.
    /// </summary>
    public required DecorStateDto Decor { get; init; }

    /// <summary>
    /// Обжитость деревни и её слагаемые.
    /// </summary>
    public required VillageLevelDto VillageLevel { get; init; }

    /// <summary>
    /// Обозы соседей, открытых по обжитости деревни, с ассортиментом и остатком суточного лимита.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Economy.ConvoyManager.GetConvoys"/>.
    /// </remarks>
    public required ConvoyDto[] Convoys { get; init; }

    /// <summary>
    /// Текущая толока и вклад игрока в неё.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – толока ещё недоступна игроку. Дельта вклада с прошлого захода
    /// (<see cref="TolokaStateDto.Progress"/>) здесь всегда пуста: её забирает только снимок состояния игры.
    /// </remarks>
    public TolokaStateDto? Toloka { get; init; }

    /// <summary>
    /// Общее состояние деревни игрока.
    /// </summary>
    public required VillageDto Village { get; init; }

    /// <summary>
    /// Справочник уклада деревни: соседи и постройки их специализации.
    /// </summary>
    /// <remarks>
    /// См. <see cref="Reference.ResourceManager.GetVillageProfileEffects"/>.
    /// </remarks>
    public required VillageProfileDto[] VillageProfiles { get; init; }

    /// <summary>
    /// Репутация игрока у всех соседей.
    /// </summary>
    public required NeighborReputationDto[] Reputation { get; init; }

    /// <summary>
    /// Переезд в новую долину: гейт, узелки памяти и лесенка перков.
    /// </summary>
    /// <remarks>
    /// Справочнику нужен живой порог обжитости (<see cref="RelocationDto.Threshold"/>): он растёт с каждым переездом
    /// (см. <see cref="Village.VillageLevelCalculator.GetRelocationThreshold"/>), поэтому статьёй его не расскажешь.
    /// </remarks>
    public required RelocationDto Relocation { get; init; }
}
