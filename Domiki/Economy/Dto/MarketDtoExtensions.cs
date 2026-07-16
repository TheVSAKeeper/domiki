using Domiki.Web.Economy.Models;

namespace Domiki.Web.Economy.Dto;

public static class MarketDtoExtensions
{
    public static MarketStateDto ToDto(this MarketState state)
    {
        return new()
        {
            Lots = state.Lots.Select(x => x.ToDto()).ToArray(),
            MyLots = state.MyLots.Select(x => x.ToDto()).ToArray(),
            BuildingLevel = state.BuildingLevel,
            CommissionRate = state.CommissionRate,
            CommissionMin = state.CommissionMin,
            NextCommissionRate = state.NextCommissionRate,
            MaxLots = state.MaxLots,
        };
    }

    public static TradeLotDto ToDto(this TradeLot lot)
    {
        return new()
        {
            Id = lot.Id,
            SellerId = lot.SellerId,
            SellerVillageName = lot.SellerVillageName,
            SellerCrestIcon = lot.SellerCrestIcon,
            SellerCrestColor = lot.SellerCrestColor,
            GiveResourceTypeId = lot.GiveResourceTypeId,
            GiveValue = lot.GiveValue,
            WantResourceTypeId = lot.WantResourceTypeId,
            WantValue = lot.WantValue,
            CommissionCoins = lot.CommissionCoins,
            ExpireDate = DateTime.SpecifyKind(lot.ExpireDate, DateTimeKind.Utc),
        };
    }
}
