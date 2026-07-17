namespace Domiki.Web.Infrastructure.Dto;

/// <summary>
/// Витрина «Пока вас не было».
/// </summary>
/// <remarks>
/// Непоказанные события, накопившиеся за время отсутствия игрока; выдаётся один раз и сразу помечается прочитанной
/// (см. <see cref="Infrastructure.PlayerEventManager.TakeRecap"/>).
/// </remarks>
public class RecapDto
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
    /// Может быть пустым массивом.
    /// </remarks>
    public RecapEventDto[] Events { get; set; }
}

/// <summary>
/// Одна запись игрового журнала – тип события, момент и данные.
/// </summary>
public class RecapEventDto
{
    /// <summary>
    /// Вид события.
    /// </summary>
    /// <remarks>
    /// Строковое имя значения <see cref="Data.Entities.PlayerEventType"/>; определяет формат <see cref="Data"/>.
    /// </remarks>
    public string Type { get; set; }

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
    public object Data { get; set; }
}
