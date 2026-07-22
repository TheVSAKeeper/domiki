using Domiki.Web.Workers.Models;

namespace Domiki.Web.Workers.Dto;

/// <summary>
/// Преобразует состояние плащей в контракт клиента.
/// </summary>
public static class CloakStateDtoExtensions
{
    /// <summary>
    /// Преобразует состояние плащей в DTO.
    /// </summary>
    /// <param name="state">Состояние плащей игрока.</param>
    /// <returns>Контракт состояния плащей для клиента.</returns>
    public static CloakStateDto ToDto(this CloakState state)
    {
        return new()
        {
            Stock = state.Stock,
            OutOnShifts = state.OutOnShifts,
            WearPoints = state.WearPoints,
            LifetimeShifts = state.LifetimeShifts,
        };
    }
}
