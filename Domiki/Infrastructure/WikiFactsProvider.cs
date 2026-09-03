using System.Globalization;
using Domiki.Web.Activities;
using Domiki.Web.Core;
using Domiki.Web.Economy;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Domiki.Web.Workers;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Числа внутриигрового справочника: словарь готовых к подстановке фактов, которым статьи заменяют
/// зашитые в текст числа.
/// </summary>
/// <remarks>
/// Единственный источник – константы менеджеров и справочные данные <see cref="ResourceManager"/>, поэтому балансовая
/// миграция или правка константы доезжает до статьи сама. Значения отдаются строками уже в том виде, в каком их читают
/// в русской фразе (разряды пробелом, дробь запятой, знак процента у прибавок), – клиент их не форматирует, а только
/// подставляет по имени (<c>ClientApp/src/utils/wikiFacts.ts</c>). Имя факта называет утверждение статьи, а не
/// константу: одна константа может стоять за несколькими утверждениями, и каждое зовётся по-своему.
/// Связку «на каждый факт есть подстановка, на каждую подстановку есть факт» держит тест <c>WikiFactsTest</c>.
/// Подстановками закрыты числа, записанные в статье цифрами; числа, записанные словом («наполовину», «шести золотых»,
/// «десятую часть»), остаются в тексте как есть – фразу они держат лучше цифры, и факта для них нет.
/// </remarks>
public sealed class WikiFactsProvider
{
    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3],
    };

    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly ResourceManager _resourceManager;

    /// <summary>
    /// Создаёт поставщик фактов.
    /// </summary>
    /// <param name="resourceManager">Справочники игры – источник фактов, которых нет в константах.</param>
    public WikiFactsProvider(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    /// <summary>
    /// Собирает словарь фактов для статей справочника.
    /// </summary>
    /// <returns>Имя факта – готовая к подстановке строка.</returns>
    public Dictionary<string, string> GetFacts()
    {
        var domikTypes = _resourceManager.GetDomikTypes().ToDictionary(x => x.LogicName);
        var countGates = _resourceManager.GetDomikTypeCountGates();
        var decorTypes = _resourceManager.GetDecorTypes().ToDictionary(x => x.LogicName);
        var blueprints = _resourceManager.GetBlueprints().ToDictionary(x => x.LogicName);
        var traits = _resourceManager.GetTraits().ToDictionary(x => x.LogicName);
        var weatherTypes = _resourceManager.GetWeatherTypes().ToDictionary(x => x.LogicName);
        var tolokaTypes = _resourceManager.GetTolokaTypes().ToDictionary(x => x.LogicName);
        var expeditionTypes = _resourceManager.GetExpeditionTypes().ToDictionary(x => x.LogicName);
        var resourceTypeIds = _resourceManager.GetResourceTypes()
            .Where(x => x.LogicName != null)
            .ToDictionary(x => x.LogicName!, x => x.Id);
        var coinResourceTypeId = resourceTypeIds["coin"];

        int ArtelHutLevel(int ordinal) => countGates
            .Single(gate => gate.DomikTypeId == domikTypes["barracks"].Id && gate.Ordinal == ordinal)
            .UnlockLevel;

        int DomikCoinPrice(string logicName) => domikTypes[logicName].Levels
            .Single(level => level.Value == 1)
            .Resources
            .Single(resource => resource.Type.Id == coinResourceTypeId)
            .Value;

        int DecorCoinPrice(string logicName) => decorTypes[logicName].Cost
            .Single(resource => resource.Type.Id == coinResourceTypeId)
            .Value;

        int WeatherEffect(string weatherLogicName, string domikLogicName) => weatherTypes[weatherLogicName].Effects
            .Single(effect => effect.DomikTypeId == domikTypes[domikLogicName].Id)
            .OutputPercent - 100;

        int TolokaEffect(string logicName) => tolokaTypes[logicName].Effects
            .Select(effect => effect.OutputPercent - 100)
            .Distinct()
            .Single();

        int TolokaGoal(string tolokaLogicName, string resourceLogicName) => tolokaTypes[tolokaLogicName].Positions
            .Single(position => position.ResourceTypeId == resourceTypeIds[resourceLogicName])
            .Goal;

        int ExpeditionHours(string logicName) => Hours(expeditionTypes[logicName].DurationSeconds);

        int ExpeditionGear(string logicName) => expeditionTypes[logicName].Equipment
            .Single(item => !item.IsOptional)
            .Value;

        var profileDurationPercent = _resourceManager.GetVillageProfileEffects()
            .Select(effect => effect.DurationPercent)
            .Distinct()
            .Single();

        var artisanChain = new[] { "carved_gate", "crane_well", "gazebo", "carp_pond" };

        return new Dictionary<string, string>
        {
            ["villageBuildingWeight"] = Int(VillageLevelCalculator.BuildingWeight),
            ["villageResidentWeight"] = Int(VillageLevelCalculator.ResidentWeight),
            ["villageReputationWeight"] = Int(VillageLevelCalculator.ReputationWeight),
            ["villageComfortWeight"] = Int(VillageLevelCalculator.ComfortWeight),
            ["comfortCap"] = Int(VillageLevelCalculator.ComfortHabitabilityCap),
            ["smartArtelLevel"] = Int(VillageLevelCalculator.SmartAutoUnlockLevel),
            ["villageProfileLevel"] = Int(VillageProfileManager.VillageLevelRequirement),
            ["elderHouseLevel"] = Int(domikTypes["elder_house"].UnlockLevel),
            ["relocationFirstLevel"] = Int(VillageLevelCalculator.RelocationUnlockLevel),
            ["relocationLevelStep"] = Int(VillageLevelCalculator.RelocationLevelStep),
            ["relocationMaxLevel"] = Int(VillageLevelCalculator.RelocationMaxUnlockLevel),
            ["relocationCooldownDays"] = Int(VillageLevelCalculator.RelocationCooldownDays),
            ["relocationGoldCarry"] = Int(RelocationManager.GoldCarryCap),
            ["knotsPerVillageLevel"] = Int(VillageLevelCalculator.RelocationLevelStep),
            ["artelHutSixthLevel"] = Int(ArtelHutLevel(6)),
            ["artelHutSeventhLevel"] = Int(ArtelHutLevel(7)),
            ["artelHutEighthLevel"] = Int(ArtelHutLevel(8)),
            ["perkLiftingsSteps"] = JoinAnd(PerkCosts(Data.Entities.RelocationPerkType.Liftings)),
            ["perkLiftingsCoins"] = Int(PerkManager.LiftingsCoinsPerStep),
            ["perkLongHabitSteps"] = JoinAnd(PerkCosts(Data.Entities.RelocationPerkType.LongHabit)),
            ["perkLongHabitPercent"] = Int(PerkManager.LongHabitDurationPercentPerStep),
            ["perkSpareBunkSteps"] = JoinAnd(PerkCosts(Data.Entities.RelocationPerkType.SpareBunk)),

            ["workerCap"] = Int(WorkerManager.MaxCapacity),
            ["traitNimblePercent"] = Int(Math.Abs(traits["nimble"].DurationPercent)),
            ["traitDiligentPercent"] = Int(Math.Abs(traits["diligent"].DurationPercent)),
            ["traitSonyaPercent"] = Int(Math.Abs(traits["sonya"].DurationPercent)),
            ["workerSkillMaxPercent"] = Int((int)Math.Round(WorkerSkillCalculator.MaxBonus * 100)),
            ["fatigueHours"] = Int(Hours(DomikManager.FatigueThresholdSeconds)),
            ["restHours"] = Int(Hours(DomikManager.RestSeconds)),
            ["milestoneUnlockLevel"] = Int(WorkerMilestoneManager.WorkerMilestoneUnlockLevel),
            ["milestoneCooldownHours"] = Int(WorkerMilestoneManager.WorkerMilestoneCooldownHours),
            ["milestoneHundredthShift"] = Int(WorkerMilestoneManager.HundredthShiftThreshold),
            ["milestoneSkilledHand"] = Int(WorkerMilestoneManager.SkilledHandThreshold),
            ["milestoneTwoAtBench"] = Int(WorkerMilestoneManager.TwoAtBenchThreshold),
            ["milestoneTenthRoad"] = Int(WorkerMilestoneManager.TenthRoadThreshold),
            ["milestoneMonthDays"] = Int(WorkerMilestoneManager.MonthInBarracksDays),

            ["orderBoardSizeBase"] = Int(OrderManager.BoardSizeBase),
            ["orderBoardSizeMax"] = Int(OrderManager.BoardSizeMax),
            ["orderFourthSlotLevel"] = Int(OrderManager.FourthSlotLevel),
            ["orderFifthSlotLevel"] = Int(OrderManager.FifthSlotLevel),
            ["orderFreeConcessionLevel"] = Int(OrderManager.FreeConcessionLevel),
            ["errandSecondLevel"] = Int(ErrandManager.SecondErrandLevel),
            ["orderMinQuantity"] = Int(OrderManager.MinQuantity),
            ["orderTier1Hours"] = Int(Hours(OrderManager.Tiers[0].DurationSeconds)),
            ["orderTier1Multiplier"] = Dec(OrderManager.Tiers[0].DemandMultiplier),
            ["orderTier1Reputation"] = Int(OrderManager.Tiers[0].RewardReputation),
            ["orderTier2Hours"] = Int(Hours(OrderManager.Tiers[1].DurationSeconds)),
            ["orderTier2Multiplier"] = Dec(OrderManager.Tiers[1].DemandMultiplier),
            ["orderTier2Gold"] = Int(OrderManager.Tiers[1].RewardGold),
            ["orderTier2Reputation"] = Int(OrderManager.Tiers[1].RewardReputation),
            ["orderTier3Multiplier"] = Dec(OrderManager.Tiers[2].DemandMultiplier),
            ["orderTier3Gold"] = Int(OrderManager.Tiers[2].RewardGold),
            ["orderTier3Reputation"] = Int(OrderManager.Tiers[2].RewardReputation),
            ["orderRefillMinutes"] = Int(Minutes(OrderManager.OrderRefillDelaySeconds)),

            ["profileDurationCutPercent"] = Int(100 - profileDurationPercent),
            ["profileReputationRequirement"] = Int(VillageProfileManager.ReputationRequirement),
            ["profileCooldownDays"] = Int(VillageProfileManager.ProfileChangeCooldownDays),
            ["durationFloorPercent"] = Int((int)Math.Round(DomikManager.MinDurationShare * 100)),
            ["profileExampleCutPercent"] = Dec(ProfileExampleCutPercent(traits["diligent"].DurationPercent, profileDurationPercent), 1),

            ["errandUnlockLevel"] = Int(ErrandManager.ErrandUnlockLevel),
            ["errandRotationPercent"] = Int(ErrandManager.ErrandRotationWeightPercent),
            ["errandOfferHours"] = Int(ErrandManager.ErrandOfferDurationHours),
            ["errandMaxWorkers"] = Int(ErrandManager.ErrandMaxWorkers),
            ["errandClueHours"] = JoinOr(ErrandManager.ClueDurationHours),
            ["errandClueReputation"] = JoinOr(ErrandManager.ClueReputation.Select(x => "+" + Int(x))),
            ["errandCoinsPerWorkerHour"] = Int(ErrandManager.ErrandCoinsPerWorkerHour),
            ["errandBonusFindPercent"] = Int(ErrandManager.ErrandBonusFindChancePercent),

            ["convoyPriceMultiplier"] = Int(ConvoyManager.PriceMultiplier),
            ["convoyAccessReputation"] = Int(ConvoyManager.AccessReputationThreshold),
            ["convoySecondGoodReputation"] = Int(ConvoyManager.SecondaryReputationThreshold),
            ["convoyHighLimitReputation"] = Int(ConvoyManager.HighLimitReputationThreshold),
            ["convoyBaseLimit"] = Int(ConvoyManager.BaseLimit),
            ["convoyHighLimit"] = Int(ConvoyManager.HighLimit),
            ["convoyWindowHours"] = Int(Hours(ConvoyManager.WindowDurationSeconds)),

            ["incidentChancePercent"] = Int(IncidentManager.IncidentChancePercent),
            ["incidentCooldownHours"] = Int(IncidentManager.IncidentCooldownHours),
            ["incidentMinSquad"] = Int(IncidentManager.IncidentMinSquadSize),
            ["incidentMinFreeWorkers"] = Int(IncidentManager.IncidentMinFreeWorkers),
            ["incidentAutoReturnHours"] = Int(IncidentManager.IncidentAutoReturnHours),
            ["domikIncidentUnlockLevel"] = Int(IncidentManager.DomikIncidentUnlockLevel),
            ["domikIncidentCooldownHours"] = Int(IncidentManager.DomikIncidentCooldownHours),
            ["domikIncidentMinFreeWorkers"] = Int(IncidentManager.DomikIncidentMinFreeWorkers),
            ["domikIncidentAutoResolveHours"] = Int(IncidentManager.DomikIncidentAutoResolveHours),
            ["incidentClueHours"] = JoinOr(IncidentManager.ClueDurationHours),
            ["incidentSearchMaxWorkers"] = Int(IncidentManager.IncidentSearchMaxWorkers),

            ["giftAwayHours"] = Int(Hours(GiftManager.GiftAwayThresholdSeconds)),
            ["giftRepTierNormal"] = $"0–{Int(GiftManager.RepWeightPerPoint - 1)}",
            ["giftRepTierDouble"] = $"{Int(GiftManager.RepWeightPerPoint)}–{Int(GiftManager.RepWeightPerPoint * 2 - 1)}",
            ["giftRepTierTriple"] = Int(GiftManager.RepWeightPerPoint * (GiftManager.RepWeightCap - 1)),
            ["giftBaseValue"] = Int(GiftManager.BaseGiftValue),
            ["giftBonusReputation"] = Int(GiftManager.RepBonusThreshold),
            ["giftBonusValue"] = Int(GiftManager.BaseGiftValue * 3 / 2),
            ["giftCountMin"] = Int(GiftManager.GiftCountMin),
            ["giftCountCap"] = Int(GiftManager.GiftCountCap),
            ["giftBigEvery"] = Int(GiftManager.BigGiftEvery),

            ["tavernUnlockLevel"] = Int(domikTypes["tavern"].UnlockLevel),
            ["tavernPrice"] = Int(DomikCoinPrice("tavern")),
            ["warmCornerPercent"] = Int(TavernManager.WarmCornerRecoveryPercent),

            ["ledgerKeepDays"] = Int(ElderHouseManager.LedgerKeepDays),
            ["elderForecastHours"] = Int(ElderHouseManager.ShortageHorizonHours),

            ["zealStartCharges"] = Int(DomikManager.ZealStartCharges),
            ["zealX4Threshold"] = Int(DomikManager.ZealX4Threshold),

            ["weatherPeriodHours"] = Int(Hours(WeatherManager.WeatherPeriodSeconds)),
            ["weatherWeightClear"] = Int(weatherTypes["clear"].RotationWeight),
            ["weatherWeightRain"] = Int(weatherTypes["rain"].RotationWeight),
            ["weatherWeightDrought"] = Int(weatherTypes["drought"].RotationWeight),
            ["weatherWeightFrost"] = Int(weatherTypes["frost"].RotationWeight),
            ["weatherWeightWind"] = Int(weatherTypes["wind"].RotationWeight),
            ["weatherRainClayMine"] = Signed(WeatherEffect("rain", "clay_mine")),
            ["weatherRainField"] = Signed(WeatherEffect("rain", "field")),
            ["weatherRainLumberMill"] = Signed(WeatherEffect("rain", "lumber_mill")),
            ["weatherRainMill"] = Signed(WeatherEffect("rain", "mill")),
            ["weatherDroughtLumberMill"] = Signed(WeatherEffect("drought", "lumber_mill")),
            ["weatherDroughtStoneMine"] = Signed(WeatherEffect("drought", "stone_mine")),
            ["weatherDroughtClayMine"] = Signed(WeatherEffect("drought", "clay_mine")),
            ["weatherDroughtField"] = Signed(WeatherEffect("drought", "field")),
            ["weatherFrostForge"] = Signed(WeatherEffect("frost", "forge")),
            ["weatherFrostBakery"] = Signed(WeatherEffect("frost", "bakery")),
            ["weatherFrostStoneMine"] = Signed(WeatherEffect("frost", "stone_mine")),
            ["weatherWindMill"] = Signed(WeatherEffect("wind", "mill")),
            ["weatherWindLumberMill"] = Signed(WeatherEffect("wind", "lumber_mill")),
            ["weatherWindForge"] = Signed(WeatherEffect("wind", "forge")),
            ["weatherWindBakery"] = Signed(WeatherEffect("wind", "bakery")),

            ["sickUnlockLevel"] = Int(DomikManager.SickMinVillageLevel),
            ["sickChancePerExtraUnit"] = Int(DomikManager.SickChancePerExtraUnit),
            ["sickMaxChancePercent"] = Int(DomikManager.MaxSickChancePercent),
            ["sickMinChancePercent"] = Int(DomikManager.MinSickChancePercent),
            ["sickDurationHours"] = Int(Hours(DomikManager.SickDurationSeconds)),
            ["sickImmunityHours"] = Int(Hours(DomikManager.SickImmunitySeconds)),
            ["cloakLifetimeShifts"] = Int(DomikManager.CloakLifetimeShifts),

            ["blueprintWorkshopReputation"] = Int(blueprints["workshop"].ReputationThreshold),
            ["blueprintStonecutterReputation"] = Int(blueprints["stonecutter"].ReputationThreshold),
            ["blueprintPotteryReputation"] = Int(blueprints["pottery"].ReputationThreshold),
            ["blueprintBakeryReputation"] = Int(blueprints["bakery"].ReputationThreshold),
            ["blueprintPickReputation"] = Int(blueprints["pick"].ReputationThreshold),
            ["blueprintTongsReputation"] = Int(blueprints["tongs"].ReputationThreshold),

            ["expeditionFootHours"] = Int(ExpeditionHours("foot_scout")),
            ["expeditionFootWorkers"] = Int(expeditionTypes["foot_scout"].WorkerCount),
            ["expeditionShortHours"] = Int(ExpeditionHours("short_scout")),
            ["expeditionShortWorkers"] = Int(expeditionTypes["short_scout"].WorkerCount),
            ["expeditionShortGold"] = Int(expeditionTypes["short_scout"].GoldCost),
            ["expeditionShortEquipment"] = Int(ExpeditionGear("short_scout")),
            ["expeditionLongHours"] = Int(ExpeditionHours("long_journey")),
            ["expeditionLongWorkers"] = Int(expeditionTypes["long_journey"].WorkerCount),
            ["expeditionLongGold"] = Int(expeditionTypes["long_journey"].GoldCost),
            ["expeditionLongEquipment"] = Int(ExpeditionGear("long_journey")),
            ["expeditionPityThreshold"] = Int(ExpeditionManager.ExpeditionPityThreshold),

            ["marketUnlockLevel"] = Int(domikTypes["market_yard"].UnlockLevel),
            ["marketCommissionFirstPercent"] = Dec(MarketManager.CommissionRateL1 * 100),
            ["marketCommissionStepPercent"] = Dec(MarketManager.CommissionRateStep * 100),
            ["marketCommissionMinPercent"] = Dec(MarketManager.CommissionRateMin * 100),
            ["marketMinCommissionCoins"] = Int(MarketManager.MinCommissionCoins),
            ["marketLotHours"] = Int(Hours(MarketManager.MarketLotDurationSeconds)),

            ["tolokaUnlockLevel"] = Int(domikTypes["gathering"].UnlockLevel),
            ["tolokaBaseGoal"] = Int(TolokaGoal("bridge", "stone")),
            ["tolokaCaravanBrick"] = Int(TolokaGoal("caravan", "brick")),
            ["tolokaCaravanBoard"] = Int(TolokaGoal("caravan", "board")),
            ["tolokaCaravanTool"] = Int(TolokaGoal("caravan", "tool")),
            ["tolokaBridgeBonus"] = Int(TolokaManager.BridgeOrderBonusPercent),
            ["tolokaGranaryBonus"] = Int(TolokaEffect("granary")),
            ["tolokaKilnBonus"] = Int(TolokaEffect("kiln")),
            ["tolokaCaravanBonus"] = Int(TolokaEffect("caravan")),
            ["tolokaFeastBaseHours"] = Int(TolokaManager.FeastBaseHours),
            ["tolokaFeastHoursPerLevel"] = Int(TolokaManager.FeastHoursPerLevel),
            ["tolokaFeastFirstLevelHours"] = Int(Hours(TolokaManager.GetBuffSeconds(1))),

            ["helpUnlockLevel"] = Int(HelpManager.HelpUnlockLevel),
            ["helpRewardCoins"] = Int(HelpManager.HelpRewardCoins),

            ["guestbookUnlockLevel"] = Int(GuestbookManager.GuestbookUnlockLevel),

            ["seasonDurationDays"] = Int(SeasonManager.SeasonDurationSeconds / 86400),
            ["seasonEpochDate"] = SeasonManager.SeasonEpoch.ToString("d MMMM yyyy", DateCulture),

            ["decorFencePoints"] = Signed(decorTypes["fence"].ComfortPoints),
            ["decorFlowerbedPoints"] = Signed(decorTypes["flowerbed"].ComfortPoints),
            ["decorBenchPoints"] = Signed(decorTypes["bench"].ComfortPoints),
            ["decorGardenPoints"] = Signed(decorTypes["garden"].ComfortPoints),
            ["decorLanternPoints"] = Signed(decorTypes["lantern"].ComfortPoints),
            ["decorFountainPoints"] = Signed(decorTypes["fountain"].ComfortPoints),
            ["decorBrickArchPoints"] = Signed(decorTypes["brick_arch"].ComfortPoints),
            ["decorBrickArchReputation"] = Int(decorTypes["brick_arch"].ReputationThreshold),
            ["decorCarvedGatePoints"] = Signed(decorTypes["carved_gate"].ComfortPoints),
            ["decorCarvedGatePrice"] = Int(DecorCoinPrice("carved_gate")),
            ["decorCraneWellPrice"] = Int(DecorCoinPrice("crane_well")),
            ["decorGazeboPrice"] = Int(DecorCoinPrice("gazebo")),
            ["decorCarpPondPrice"] = Int(DecorCoinPrice("carp_pond")),
            ["decorArtisanTotalPrice"] = Int(artisanChain.Sum(DecorCoinPrice)),
            ["decorTrailIdolPoints"] = Signed(decorTypes["trail_idol"].ComfortPoints),
            ["decorWandererBannerPoints"] = Signed(decorTypes["wanderer_banner"].ComfortPoints),
        };
    }

    private static int[] PerkCosts(Data.Entities.RelocationPerkType type) => PerkManager.Perks.Single(x => x.Type == type).Costs;

    private static double ProfileExampleCutPercent(int traitDurationPercent, int profileDurationPercent)
    {
        var beforeProfile = (100 + traitDurationPercent) / 100.0 * (1 - WorkerSkillCalculator.MaxBonus);
        var afterProfile = Math.Max(beforeProfile * profileDurationPercent / 100.0, DomikManager.MinDurationShare);
        return (beforeProfile - afterProfile) / beforeProfile * 100;
    }

    private static string Int(int value) => value.ToString("#,0", NumberFormat);

    /// <summary>
    /// Переводит срок из секунд в целые часы, требуя точной кратности.
    /// </summary>
    /// <param name="seconds">Срок в секундах.</param>
    /// <returns>Число целых часов.</returns>
    /// <exception cref="InvalidOperationException">Срок не кратен часу: округление подсунуло бы игроку неверное число.</exception>
    private static int Hours(int seconds) => seconds % 3600 == 0
        ? seconds / 3600
        : throw new InvalidOperationException($"Срок {seconds} с не кратен часу.");

    /// <summary>
    /// Переводит срок из секунд в целые минуты, требуя точной кратности.
    /// </summary>
    /// <param name="seconds">Срок в секундах.</param>
    /// <returns>Число целых минут.</returns>
    /// <exception cref="InvalidOperationException">Срок не кратен минуте: округление подсунуло бы игроку неверное число.</exception>
    private static int Minutes(int seconds) => seconds % 60 == 0
        ? seconds / 60
        : throw new InvalidOperationException($"Срок {seconds} с не кратен минуте.");

    private static string Dec(double value, int digits = 2) => Math.Round(value, digits, MidpointRounding.AwayFromZero).ToString("0.##", NumberFormat);

    private static string Signed(int value) => value < 0 ? "−" + Int(-value) : "+" + Int(value);

    private static string JoinAnd(IEnumerable<int> values) => JoinWith(values.Select(Int), "и");

    private static string JoinOr(IEnumerable<int> values) => JoinWith(values.Select(Int), "или");

    private static string JoinOr(IEnumerable<string> values) => JoinWith(values, "или");

    private static string JoinWith(IEnumerable<string> values, string conjunction)
    {
        var items = values.ToArray();
        return items.Length < 2
            ? string.Concat(items)
            : $"{string.Join(", ", items[..^1])} {conjunction} {items[^1]}";
    }
}
