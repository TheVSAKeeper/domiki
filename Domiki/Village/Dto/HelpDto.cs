using Domiki.Web.Village.Models;

namespace Domiki.Web.Village.Dto;

/// <summary>
/// Результат успешного «подсобить».
/// </summary>
public sealed record HelpResultDto
{
    /// <summary>
    /// Название типа постройки, где сокращена работа.
    /// </summary>
    /// <remarks>
    /// Для производства – имя типа домика, в котором оно идёт, не название рецепта.
    /// </remarks>
    public required string DomikTypeName { get; init; }

    /// <summary>
    /// На сколько секунд сокращён остаток работы.
    /// </summary>
    /// <value>Секунды.</value>
    public required int ReducedSeconds { get; init; }

    /// <summary>
    /// Монеты, выданные гостю в благодарность.
    /// </summary>
    public required int RewardCoins { get; init; }
}

public static class HelpDtoExtensions
{
    public static HelpResultDto ToDto(this HelpResult result)
    {
        return new()
        {
            DomikTypeName = result.DomikTypeName,
            ReducedSeconds = result.ReducedSeconds,
            RewardCoins = result.RewardCoins,
        };
    }
}
