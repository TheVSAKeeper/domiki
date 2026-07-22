using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

/// <summary>
/// Преобразует справочник хворей в контракт клиента.
/// </summary>
public static class SickTypeDtoExtensions
{
    /// <summary>
    /// Преобразует тип хвори в DTO.
    /// </summary>
    /// <param name="sickType">Тип хвори из справочника.</param>
    /// <returns>Контракт типа хвори для клиента.</returns>
    public static SickTypeDto ToDto(this SickType sickType)
    {
        return new()
        {
            Id = sickType.Id,
            Name = sickType.Name,
            LogicName = sickType.LogicName,
            WeatherTypeId = sickType.WeatherTypeId,
            CloakProtects = sickType.CloakProtects,
        };
    }
}
