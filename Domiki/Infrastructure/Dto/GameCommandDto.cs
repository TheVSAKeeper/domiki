using System.Text.Json;

namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Одно намерение игрока в пачке: что сделать и под каким идентификатором.
/// </summary>
/// <remarks>
/// Идентификатор присваивает клиент и не меняет его при повторной отправке, поэтому доехавшая дважды пачка применяется
/// один раз (см. <see cref="Data.Entities.PlayerCommand"/>). Разбор <see cref="Args"/> лежит на
/// <see cref="GameCommandRegistry"/>: у каждого вида команды свой набор полей.
/// </remarks>
public sealed record GameCommandDto
{
    /// <summary>
    /// Идентификатор команды, присвоенный клиентом.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Вид команды – ключ реестра (<c>StartManufacture</c>, <c>CompleteOrder</c> и прочие).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Аргументы команды: объект, поля которого зависят от вида.
    /// </summary>
    public required JsonElement Args { get; init; }
}

/// <summary>
/// Пачка намерений игрока.
/// </summary>
public sealed record GameCommandBatchDto
{
    /// <summary>
    /// Игрок, у которого эти намерения родились, по его же снимку состояния.
    /// </summary>
    /// <remarks>
    /// На общем устройстве очередь намерений одного игрока переживает вход другого, поэтому пачка называет своего
    /// хозяина и отбивается целиком, если он разошёлся с текущей сессией. Старый клиент поля не шлёт, и тогда проверка
    /// не делается: <c>null</c> значит «не знаю», а не «любой».
    /// </remarks>
    public int? PlayerId { get; init; }

    /// <summary>
    /// Команды в том порядке, в каком их сделал игрок.
    /// </summary>
    public required GameCommandDto[] Commands { get; init; }
}

/// <summary>
/// Чем закончилась одна команда пачки.
/// </summary>
public enum GameCommandStatus
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Команда применена.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// Команда уже была применена раньше – повтор пропущен.
    /// </summary>
    Duplicate = 2,

    /// <summary>
    /// Команда отвергнута: мир изменился, и предусловие больше не выполняется.
    /// </summary>
    Rejected = 3,
}

/// <summary>
/// Итог одной команды пачки.
/// </summary>
public sealed record GameCommandResultDto
{
    /// <summary>
    /// Идентификатор команды.
    /// </summary>
    public required Guid CommandId { get; init; }

    /// <summary>
    /// Чем закончилась команда.
    /// </summary>
    /// <value>Строковое имя значения <see cref="GameCommandStatus"/>.</value>
    public required string Status { get; init; }

    /// <summary>
    /// Текст отказа для игрока.
    /// </summary>
    /// <remarks>
    /// Заполняется только у <see cref="GameCommandStatus.Rejected"/>; текст берётся из <see cref="BusinessException"/>
    /// и уже написан для чтения игроком.
    /// </remarks>
    public required string? Error { get; init; }
}

/// <summary>
/// Ответ на пачку намерений: итог по каждой команде и состояние после всех применённых.
/// </summary>
public sealed record GameCommandBatchResultDto
{
    /// <summary>
    /// Итоги команд в порядке отправки.
    /// </summary>
    public required GameCommandResultDto[] Results { get; init; }

    /// <summary>
    /// Состояние игрока после пачки.
    /// </summary>
    /// <remarks>
    /// Собирается <see cref="GameStateProjector"/>, то есть без одноразовых выдач.
    /// </remarks>
    public required GameStateDto State { get; init; }
}
