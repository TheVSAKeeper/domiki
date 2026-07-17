namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Витрина «Пока вас не было».
/// </summary>
/// <remarks>
/// Непоказанные события, накопившиеся за время отсутствия игрока; выдаётся один раз и сразу помечается прочитанной
/// (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
/// </remarks>
public sealed record RecapDto
{
    /// <summary>
    /// Сколько секунд игрок отсутствовал с прошлого захода.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// <c>0</c> для первого захода нового игрока.
    /// </remarks>
    public required int AwaySeconds { get; init; }

    /// <summary>
    /// Непоказанные события в хронологическом порядке.
    /// </summary>
    /// <remarks>
    /// Может быть пустым массивом.
    /// </remarks>
    public required RecapEventDto[] Events { get; init; }
}

/// <summary>
/// Одна запись игрового журнала – тип события, момент и данные.
/// </summary>
public sealed record RecapEventDto
{
    /// <summary>
    /// Вид события.
    /// </summary>
    /// <remarks>
    /// Строковое имя значения <see cref="Data.Entities.PlayerEventType"/>; определяет формат <see cref="Data"/>.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Момент события.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Данные события в формате, специфичном для <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Например, для <see cref="Data.Entities.PlayerEventType.ManufactureFinished"/> – накопленные ресурсы и число циклов
    /// (см. <see cref="Infrastructure.PlayerEventManager.RecordManufactureFinished"/>).
    /// </remarks>
    public required object Data { get; init; }
}
