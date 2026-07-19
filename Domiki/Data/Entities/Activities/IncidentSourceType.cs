namespace Domiki.Web.Data.Entities;

/// <summary>
/// Источник завязки происшествия.
/// </summary>
public enum IncidentSourceType
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// Пропажа трудяги в походе.
    /// </summary>
    Expedition = 1,

    /// <summary>
    /// Загадка в постройке.
    /// </summary>
    Domik = 2,
}
