namespace Domiki.Web.Core.Models;

/// <summary>
/// Исходные данные для расчёта длительности смены.
/// </summary>
/// <remarks>
/// Собирается в <see cref="DomikManager.StartManufacture"/> из отобранных трудяг, уклада деревни и лесенки перков.
/// Отдельная запись нужна, чтобы расчёт можно было воспроизвести на клиенте: те же поля приходят в снимке состояния,
/// а совпадение результата закрыто вектором <c>ClientApp/src/utils/manufactureDurationVectors.ts</c>.
/// </remarks>
public sealed record ManufactureDurationInput
{
    /// <summary>
    /// Базовая длительность рецепта в секундах.
    /// </summary>
    public required int ReceiptDurationSeconds { get; init; }

    /// <summary>
    /// Черта каждого отобранного трудяги в процентах к длительности: отрицательное значение ускоряет смену.
    /// </summary>
    public required int[] TraitDurationPercents { get; init; }

    /// <summary>
    /// Навык каждого отобранного трудяги в этой постройке, проценты сокращения.
    /// </summary>
    public required int[] SkillBonusPercents { get; init; }

    /// <summary>
    /// Множитель уклада деревни для этой постройки в процентах: 100 – уклад не выбран или постройке не помогает.
    /// </summary>
    public required int ProfilePercent { get; init; }

    /// <summary>
    /// Множитель перка «Долгая привычка» в процентах.
    /// </summary>
    public required int PerkPercent { get; init; }

    /// <summary>
    /// Постройка – Лавка: рвение на неё не распространяется.
    /// </summary>
    public required bool IsMarketDomik { get; init; }

    /// <summary>
    /// Запас зарядов рвения у игрока.
    /// </summary>
    public required int ZealCharges { get; init; }
}
