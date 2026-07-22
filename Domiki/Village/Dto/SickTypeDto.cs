namespace Domiki.Web.Village.Dto;

/// <summary>
/// Справочник хвори, связанной с погодой.
/// </summary>
public sealed record SickTypeDto
{
    /// <summary>
    /// Идентификатор типа хвори.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Отображаемое название хвори.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Техническое имя хвори.
    /// </summary>
    public required string LogicName { get; init; }

    /// <summary>
    /// Тип погоды, вызывающий эту хворь.
    /// </summary>
    public required int WeatherTypeId { get; init; }

    /// <summary>
    /// Бережёт ли плащ от этой хвори.
    /// </summary>
    public required bool CloakProtects { get; init; }
}
