using Domiki.Web.Data.Entities;
using System.Text.Json;

namespace Domiki.Web.Infrastructure.Models;

/// <summary>
/// Витрина «Пока вас не было».
/// </summary>
/// <remarks>
/// Непоказанные события, накопившиеся за время отсутствия игрока; собирается и сразу помечается прочитанной в
/// <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>, отдаётся на клиент как <see cref="Dto.RecapDto"/>.
/// </remarks>
public class RecapModel
{
    /// <summary>
    /// Сколько секунд игрок отсутствовал с прошлого захода.
    /// </summary>
    /// <value>Секунды.</value>
    /// <remarks>
    /// <c>0</c> для первого захода нового игрока.
    /// </remarks>
    public int AwaySeconds { get; set; }

    /// <summary>
    /// Непоказанные события в хронологическом порядке.
    /// </summary>
    /// <remarks>
    /// Может быть пустым списком.
    /// </remarks>
    public List<RecapEventModel> Events { get; set; } = [];
}

/// <summary>
/// Одна запись игрового журнала – тип события, момент и данные.
/// </summary>
public class RecapEventModel
{
    /// <summary>
    /// Вид события.
    /// </summary>
    /// <remarks>
    /// Определяет формат <see cref="Data"/>.
    /// </remarks>
    public PlayerEventType Type { get; set; }

    /// <summary>
    /// Момент события.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime Date { get; set; }

    /// <summary>
    /// Данные события в формате, специфичном для <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Например, для <see cref="Data.Entities.PlayerEventType.ManufactureFinished"/> – накопленные ресурсы и число циклов
    /// (см. <see cref="Infrastructure.PlayerEventManager.RecordManufactureFinished"/>).
    /// </remarks>
    public JsonElement Data { get; set; }
}
