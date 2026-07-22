namespace Domiki.Web.Village.Models;

/// <summary>
/// Вид хвори, вызываемой конкретной погодой.
/// </summary>
public class SickType
{
    /// <summary>
    /// Идентификатор типа хвори.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название хвори.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Техническое имя хвори, используемое на клиенте.
    /// </summary>
    public required string LogicName { get; set; }

    /// <summary>
    /// Тип погоды, вызывающий эту хворь.
    /// </summary>
    public int WeatherTypeId { get; set; }

    /// <summary>
    /// Бережёт ли плащ от этой хвори.
    /// </summary>
    public bool CloakProtects { get; set; }
}
