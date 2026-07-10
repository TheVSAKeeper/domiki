using System.Globalization;
using System.Text;
using Domiki.Web.Business.Core;
using Domiki.Web.Business.Models;

namespace Domiki.BalanceSim;

public enum ScenarioKind
{
    None = 0,
    Casual = 1,
    Optimal = 2,
    Extreme = 3,
}

public enum SimulationEventKind
{
    None = 0,
    WeatherBoundary = 1,
    ManufactureFinished = 2,
    DomikFinished = 3,
    ExpeditionReturned = 4,
    OrderExpired = 5,
    Login = 6,
}

public sealed class SimulationData
{
    public required DomikType[] DomikTypes { get; init; }
    public required Receipt[] Receipts { get; init; }
    public required ResourceType[] ResourceTypes { get; init; }
    public required Neighbor[] Neighbors { get; init; }
    public required Blueprint[] Blueprints { get; init; }
    public required WeatherType[] WeatherTypes { get; init; }
    public required ExpeditionType[] ExpeditionTypes { get; init; }
    public required Trait[] Traits { get; init; }
    public required ModificatorType[] ModificatorTypes { get; init; }
    public required Dictionary<int, DomikType> DomikTypeById { get; init; }
    public required Dictionary<int, Receipt> ReceiptById { get; init; }
    public required Dictionary<int, ResourceType> ResourceTypeById { get; init; }
    public required Dictionary<int, Neighbor> NeighborById { get; init; }
    public required Dictionary<int, WeatherType> WeatherTypeById { get; init; }
    public required Dictionary<int, Trait> TraitById { get; init; }
    public required int PlodderModificatorId { get; init; }

    public static SimulationData Load(ResourceManager resourceManager)
    {
        var domikTypes = resourceManager.GetDomikTypes().OrderBy(x => x.Id).ToArray();
        var receipts = resourceManager.GetReceipts().OrderBy(x => x.Id).ToArray();
        var resourceTypes = resourceManager.GetResourceTypes().OrderBy(x => x.Id).ToArray();
        var neighbors = resourceManager.GetNeighbors().OrderBy(x => x.Id).ToArray();
        var blueprints = resourceManager.GetBlueprints().OrderBy(x => x.Id).ToArray();
        var weatherTypes = resourceManager.GetWeatherTypes().OrderBy(x => x.Id).ToArray();
        var expeditionTypes = resourceManager.GetExpeditionTypes().OrderBy(x => x.Id).ToArray();
        var traits = resourceManager.GetTraits().OrderBy(x => x.Id).ToArray();
        var modificatorTypes = resourceManager.GetModificatorTypes().OrderBy(x => x.Id).ToArray();
        var plodder = modificatorTypes.Single(x => x.LogicName == "plodder").Id;

        return new SimulationData
        {
            DomikTypes = domikTypes,
            Receipts = receipts,
            ResourceTypes = resourceTypes,
            Neighbors = neighbors,
            Blueprints = blueprints,
            WeatherTypes = weatherTypes,
            ExpeditionTypes = expeditionTypes,
            Traits = traits,
            ModificatorTypes = modificatorTypes,
            DomikTypeById = domikTypes.ToDictionary(x => x.Id),
            ReceiptById = receipts.ToDictionary(x => x.Id),
            ResourceTypeById = resourceTypes.ToDictionary(x => x.Id),
            NeighborById = neighbors.ToDictionary(x => x.Id),
            WeatherTypeById = weatherTypes.ToDictionary(x => x.Id),
            TraitById = traits.ToDictionary(x => x.Id),
            PlodderModificatorId = plodder,
        };
    }
}

public sealed class BalanceSimulator
{
    private readonly SimulationData _data;

    public BalanceSimulator(SimulationData data)
    {
        _data = data;
    }

    public SimulationReport Run()
    {
        var runs = new Dictionary<ScenarioKind, List<SimulationRunResult>>();
        foreach (var scenario in new[] { ScenarioKind.Casual, ScenarioKind.Optimal, ScenarioKind.Extreme })
        {
            runs[scenario] = Enumerable.Range(1, 7)
                .Select(seed => new SimulationRun(_data, scenario, seed).Run())
                .ToList();
        }

        return new SimulationReport(runs);
    }
}

public sealed class SimulationReport
{
    public SimulationReport(IReadOnlyDictionary<ScenarioKind, List<SimulationRunResult>> runs)
    {
        Runs = runs;
    }

    public IReadOnlyDictionary<ScenarioKind, List<SimulationRunResult>> Runs { get; }
}

public sealed class SimulationRunResult
{
    public required int Seed { get; init; }
    public required Dictionary<int, int> VillageLevelTimes { get; init; }
    public required Dictionary<int, int> FirstDomikTimes { get; init; }
    public required Dictionary<int, int> MaxDomikLevelTimes { get; init; }
    public required Dictionary<int, int> NeighborOpenTimes { get; init; }
    public required Dictionary<int, int> BlueprintTimes { get; init; }
    public required Dictionary<int, int> FinalResources { get; init; }
    public int? ContentCompleteTime { get; set; }
    public int MaxVillageLevel { get; set; }
    public double IdleShare { get; set; }
    public double RestShare { get; set; }
    public int LongestStallSeconds { get; set; }
}

internal sealed class SimulationRun
{
    private const int HorizonSeconds = 45 * 24 * 60 * 60;
    private const int StallWarningSeconds = 24 * 60 * 60;
    private const int BuyResourceCoinReserveMultiplier = 20;

    private readonly SimulationData _data;
    private readonly ScenarioKind _scenario;
    private readonly Random _random;
    private readonly PriorityQueue<SimulationEvent, EventPriority> _events = new();
    private readonly SimulationState _state = new();
    private readonly SimulationRunResult _result;
    private long _eventSequence;
    private int _now;
    private int _lastActionTime;
    private bool _finished;

    public SimulationRun(SimulationData data, ScenarioKind scenario, int seed)
    {
        _data = data;
        _scenario = scenario;
        _random = new Random(seed);
        _result = new SimulationRunResult
        {
            Seed = seed,
            VillageLevelTimes = [],
            FirstDomikTimes = [],
            MaxDomikLevelTimes = [],
            NeighborOpenTimes = [],
            BlueprintTimes = [],
            FinalResources = [],
        };
    }

    public SimulationRunResult Run()
    {
        AddResource(1, DomikManager.StartingCoins);
        _state.CurrentWeatherTypeId = PickWeatherType().Id;
        ObserveVillageLevel();
        Schedule(SimulationEventKind.WeatherBoundary, WeatherManager.WeatherPeriodSeconds, null);
        ScheduleLogins();

        while (_events.TryDequeue(out var simulationEvent, out _))
        {
            AdvanceTo(simulationEvent.Time);
            _now = simulationEvent.Time;
            Handle(simulationEvent);
            if (_finished)
            {
                break;
            }
        }

        if (!_finished)
        {
            AdvanceTo(HorizonSeconds);
            _now = HorizonSeconds;
        }

        FinishResult();
        return _result;
    }

    private void ScheduleLogins()
    {
        for (var day = 0; day < 45; day++)
        {
            foreach (var secondOfDay in GetLoginSeconds())
            {
                Schedule(SimulationEventKind.Login, day * 24 * 60 * 60 + secondOfDay, null);
            }
        }
    }

    private IEnumerable<int> GetLoginSeconds()
    {
        return _scenario switch
        {
            ScenarioKind.Casual => [8 * 3600, 20 * 3600 + 30 * 60],
            ScenarioKind.Optimal => [8 * 3600, 13 * 3600, 18 * 3600, 22 * 3600],
            ScenarioKind.Extreme => Enumerable.Range(16, 33).Select(x => x * 30 * 60),
            _ => Array.Empty<int>(),
        };
    }

    private void Schedule(SimulationEventKind kind, int time, object? target)
    {
        if (time > HorizonSeconds)
        {
            return;
        }

        var simulationEvent = new SimulationEvent { Kind = kind, Time = time, Target = target };
        _events.Enqueue(simulationEvent, new EventPriority(time, GetEventRank(kind), _eventSequence++));
    }

    private static int GetEventRank(SimulationEventKind kind)
    {
        return kind switch
        {
            SimulationEventKind.WeatherBoundary => 0,
            SimulationEventKind.ManufactureFinished => 1,
            SimulationEventKind.DomikFinished => 2,
            SimulationEventKind.ExpeditionReturned => 3,
            SimulationEventKind.OrderExpired => 4,
            SimulationEventKind.Login => 5,
            _ => 6,
        };
    }

    private void Handle(SimulationEvent simulationEvent)
    {
        switch (simulationEvent.Kind)
        {
            case SimulationEventKind.WeatherBoundary:
                _state.CurrentWeatherTypeId = PickWeatherType().Id;
                Schedule(SimulationEventKind.WeatherBoundary, _now + WeatherManager.WeatherPeriodSeconds, null);
                break;
            case SimulationEventKind.ManufactureFinished:
                FinishManufacture((SimManufacture)simulationEvent.Target!);
                break;
            case SimulationEventKind.DomikFinished:
                FinishDomik((SimDomik)simulationEvent.Target!);
                break;
            case SimulationEventKind.ExpeditionReturned:
                FinishExpedition((SimExpedition)simulationEvent.Target!);
                break;
            case SimulationEventKind.OrderExpired:
                ExpireOrder((SimOrder)simulationEvent.Target!);
                break;
            case SimulationEventKind.Login:
                Login();
                break;
        }
    }

    private void AdvanceTo(int target)
    {
        var duration = target - _now;
        if (duration <= 0)
        {
            return;
        }

        foreach (var worker in _state.Workers)
        {
            _state.TotalWorkerSeconds += duration;
            if (worker.IsBusy)
            {
                continue;
            }

            var restSeconds = worker.RestUntil is int restUntil && restUntil > _now
                ? Math.Min(target, restUntil) - _now
                : 0;
            _state.RestWorkerSeconds += restSeconds;
            _state.FreeWorkerSeconds += duration - restSeconds;
        }
    }

    private void Login()
    {
        EnsureOrderBoard();
        var actions = CompleteOrders();
        EnsureBlueprints();
        while (BuyOrUpgrade())
        {
            actions = true;
        }

        if (StartIdleManufactures())
        {
            actions = true;
        }

        if (StartExpeditions())
        {
            actions = true;
        }

        if (actions)
        {
            RecordAction();
        }
    }

    private bool CompleteOrders()
    {
        var completed = false;
        foreach (var order in _state.Orders.OrderBy(x => x.ExpireAt).ThenBy(x => x.Id).ToArray())
        {
            if (!CanAfford(order.ResourceTypeId, order.Quantity))
            {
                continue;
            }

            if (_scenario != ScenarioKind.Casual && !CanCompleteWithoutBreakingRepeat(order))
            {
                continue;
            }

            RemoveResource(order.ResourceTypeId, order.Quantity);
            AddResource(1, order.RewardCoins);
            AddResource(5, order.RewardGold);
            _state.Reputation[order.Neighbor.Id] = GetReputation(order.Neighbor.Id) + order.RewardReputation;
            _state.Orders.Remove(order);
            EnsureOrderBoard();
            EnsureBlueprints();
            ObserveVillageLevel();
            completed = true;
        }

        return completed;
    }

    private bool CanCompleteWithoutBreakingRepeat(SimOrder order)
    {
        var reserved = _state.Domiks
            .SelectMany(x => x.Manufactures)
            .Where(x => x.AutoRepeat)
            .SelectMany(x => GetInputs(x.Receipt, x.UseOptional))
            .Where(x => x.Type.Id == order.ResourceTypeId)
            .Sum(x => x.Value);

        return _state.Resources.GetValueOrDefault(order.ResourceTypeId) - order.Quantity >= reserved;
    }

    private void EnsureOrderBoard()
    {
        while (_state.Orders.Count < OrderManager.BoardSize)
        {
            var openNeighbors = _data.Neighbors.Where(x => x.UnlockLevel <= GetVillageLevel()).ToArray();
            if (openNeighbors.Length == 0)
            {
                return;
            }

            var neighbor = openNeighbors[_random.Next(openNeighbors.Length)];
            var tier = OrderManager.Tiers[_random.Next(OrderManager.Tiers.Length)];
            var quantity = OrderManager.GetOrderQuantity(tier, neighbor.PrimaryResourceTypeId);
            var order = new SimOrder
            {
                Id = ++_state.NextOrderId,
                Neighbor = neighbor,
                ResourceTypeId = neighbor.PrimaryResourceTypeId,
                Quantity = quantity,
                RewardCoins = (int)Math.Round(quantity * ResourceManager.GetMarketValue(neighbor.PrimaryResourceTypeId) * tier.DemandMultiplier, MidpointRounding.AwayFromZero),
                RewardGold = tier.RewardGold,
                RewardReputation = tier.RewardReputation,
                ExpireAt = _now + tier.DurationSeconds,
            };
            _state.Orders.Add(order);
            Schedule(SimulationEventKind.OrderExpired, order.ExpireAt, order);
        }
    }

    private void ExpireOrder(SimOrder order)
    {
        if (!_state.Orders.Remove(order))
        {
            return;
        }

        EnsureOrderBoard();
    }

    private void EnsureBlueprints()
    {
        foreach (var blueprint in _data.Blueprints)
        {
            if (_state.OwnedBlueprints.Contains(blueprint.Id) || GetReputation(blueprint.NeighborId) < blueprint.ReputationThreshold)
            {
                continue;
            }

            _state.OwnedBlueprints.Add(blueprint.Id);
            _result.BlueprintTimes.TryAdd(blueprint.Id, _now);
        }
    }

    private bool BuyOrUpgrade()
    {
        var candidates = GetCandidates()
            .Where(x => CanAffordCandidate(x))
            .ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        var candidate = SelectCandidate(candidates);
        DeductResources(candidate.Level.Resources);
        if (candidate.Domik == null)
        {
            var domik = new SimDomik
            {
                Id = ++_state.NextDomikId,
                Type = candidate.Type,
                Level = 0,
                UpgradeFinishAt = _now + candidate.Level.UpgradeSeconds,
            };
            _state.Domiks.Add(domik);
            Schedule(SimulationEventKind.DomikFinished, domik.UpgradeFinishAt.Value, domik);
        }
        else
        {
            candidate.Domik.UpgradeFinishAt = _now + candidate.Level.UpgradeSeconds;
            Schedule(SimulationEventKind.DomikFinished, candidate.Domik.UpgradeFinishAt.Value, candidate.Domik);
        }

        return true;
    }

    private IEnumerable<DomikCandidate> GetCandidates()
    {
        var villageLevel = GetVillageLevel();
        foreach (var type in _data.DomikTypes)
        {
            if (type.UnlockLevel > villageLevel || RequiresBlueprint(type) && !HasBlueprint(type))
            {
                continue;
            }

            if (_state.Domiks.Count(x => x.Type.Id == type.Id) < type.MaxCount)
            {
                yield return new DomikCandidate(type, null, type.Levels.Single(x => x.Value == 1));
            }
        }

        foreach (var domik in _state.Domiks.Where(x => x.Level > 0 && x.UpgradeFinishAt == null))
        {
            if (domik.Level >= domik.Type.MaxLevel)
            {
                continue;
            }

            yield return new DomikCandidate(domik.Type, domik, domik.Type.Levels.Single(x => x.Value == domik.Level + 1));
        }
    }

    private bool CanAffordCandidate(DomikCandidate candidate)
    {
        if (!CanAffordResources(candidate.Level.Resources))
        {
            return false;
        }

        if (_scenario == ScenarioKind.Casual)
        {
            return true;
        }

        var coinCost = candidate.Level.Resources.Where(x => x.Type.Id == 1).Sum(x => x.Value);
        return GetResource(1) - coinCost >= GetCoinReserve();
    }

    private DomikCandidate SelectCandidate(DomikCandidate[] candidates)
    {
        if (!_state.Domiks.Any(x => x.Type.LogicName == "market"))
        {
            var market = candidates.FirstOrDefault(x => x.Type.LogicName == "market");
            if (market != null)
            {
                return market;
            }
        }

        if (_scenario == ScenarioKind.Casual)
        {
            return candidates
                .OrderBy(GetCandidateCost)
                .ThenBy(x => x.Type.Id)
                .ThenBy(x => x.Domik?.Id ?? 0)
                .First();
        }

        var barrack = candidates
            .Where(x => CapacityIncrease(x) > 0 && HasWorkerShortage())
            .OrderBy(GetCandidateCost)
            .ThenBy(x => x.Type.Id)
            .ThenBy(x => x.Domik?.Id ?? 0)
            .FirstOrDefault();
        if (barrack != null)
        {
            return barrack;
        }

        return candidates
            .OrderBy(GetPaybackHours)
            .ThenBy(GetCandidateCost)
            .ThenBy(x => x.Type.Id)
            .ThenBy(x => x.Domik?.Id ?? 0)
            .First();
    }

    private bool HasWorkerShortage()
    {
        var free = GetFreeWorkers().Count;
        return _state.Domiks.Any(d => d.Level > 0
            && d.Manufactures.Count < GetDomikLevel(d).MaxManufactureCount
            && GetReceipts(d).Any(r => r.PlodderCount > free));
    }

    private int CapacityIncrease(DomikCandidate candidate)
    {
        var next = GetCapacity(candidate.Type, candidate.Level);
        var current = candidate.Domik == null ? 0 : GetCapacity(candidate.Domik.Type, GetDomikLevel(candidate.Domik));
        return next - current;
    }

    private double GetPaybackHours(DomikCandidate candidate)
    {
        var future = GetBestReceiptEv(candidate.Type, candidate.Level) * candidate.Level.MaxManufactureCount;
        var current = candidate.Domik == null
            ? 0
            : GetBestReceiptEv(candidate.Domik.Type, GetDomikLevel(candidate.Domik)) * GetDomikLevel(candidate.Domik).MaxManufactureCount;
        var gain = future - current;
        return gain <= 0 ? double.PositiveInfinity : GetCandidateCost(candidate) / gain;
    }

    private double GetCandidateCost(DomikCandidate candidate)
    {
        return candidate.Level.Resources.Sum(x => x.Value * ResourceManager.GetMarketValue(x.Type.Id));
    }

    private int GetCoinReserve()
    {
        var mediumTier = OrderManager.Tiers[OrderManager.Tiers.Length / 2];
        var target = (int)Math.Round(_data.Neighbors.Average(neighbor =>
            OrderManager.GetOrderQuantity(mediumTier, neighbor.PrimaryResourceTypeId)
            * ResourceManager.GetMarketValue(neighbor.PrimaryResourceTypeId)
            * mediumTier.DemandMultiplier), MidpointRounding.AwayFromZero);
        var coins = GetResource(1);
        return coins < 100 ? 0 : Math.Min(target, coins / 2);
    }

    private bool StartIdleManufactures()
    {
        var started = TryStartNeededPurchase();
        var progress = true;
        while (progress)
        {
            progress = false;
            foreach (var domik in _state.Domiks.OrderBy(x => x.Type.Id).ThenBy(x => x.Id))
            {
                while (domik.Level > 0 && domik.Manufactures.Count < GetDomikLevel(domik).MaxManufactureCount)
                {
                    var selected = SelectReceiptForStart(domik);
                    if (selected == null || !StartManufacture(domik, selected.Value.Receipt, selected.Value.UseOptional, null))
                    {
                        break;
                    }

                    started = true;
                    progress = true;
                }
            }
        }

        return started;
    }

    private bool TryStartNeededPurchase()
    {
        var blocked = GetCandidates()
            .Where(candidate => !CanAffordResources(candidate.Level.Resources))
            .ToArray();
        if (blocked.Length == 0)
        {
            return false;
        }

        var target = SelectCandidate(blocked);
        var missingResource = target.Level.Resources
            .Where(resource => GetResource(resource.Type.Id) < resource.Value)
            .Select(resource => resource.Type.Id)
            .FirstOrDefault(resourceTypeId => resourceTypeId is 2 or 3 or 4);
        if (missingResource == 0)
        {
            return false;
        }

        var market = _state.Domiks
            .Where(domik => domik.Type.LogicName == "market" && domik.Level > 0 && domik.Manufactures.Count < GetDomikLevel(domik).MaxManufactureCount)
            .OrderBy(domik => domik.Id)
            .FirstOrDefault();
        if (market == null)
        {
            return false;
        }

        var receipt = GetReceipts(market)
            .FirstOrDefault(candidate => candidate.LogicName.StartsWith("buy_")
                && candidate.OutputResources.Single().Type.Id == missingResource);
        if (receipt == null || GetFreeWorkers().Count < receipt.PlodderCount)
        {
            return false;
        }

        var coinCost = receipt.InputResources.Single(resource => resource.Type.Id == 1).Value;
        return GetResource(1) >= coinCost * BuyResourceCoinReserveMultiplier
            && StartManufacture(market, receipt, false, null);
    }

    private (Receipt Receipt, bool UseOptional)? SelectReceiptForStart(SimDomik domik)
    {
        var candidates = GetReceipts(domik)
            .Select(receipt => (Receipt: receipt, UseOptional: ShouldUseOptional(domik.Type.Id, receipt)))
            .Where(x => CanAffordResources(GetInputs(x.Receipt, x.UseOptional)) && GetFreeWorkers().Count >= x.Receipt.PlodderCount)
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        if (_scenario == ScenarioKind.Casual)
        {
            var forOrder = candidates.FirstOrDefault(x => _state.Orders.Any(order =>
                GetResource(order.ResourceTypeId) < order.Quantity
                && x.Receipt.OutputResources.Any(output => output.Type.Id == order.ResourceTypeId)));
            if (forOrder.Receipt != null)
            {
                return forOrder;
            }

            if (GetResource(1) < 100)
            {
                var bestSale = candidates
                    .Where(x => x.Receipt.OutputResources.All(output => output.Type.Id == 1))
                    .OrderByDescending(x => x.Receipt.OutputResources.Sum(output => output.Value))
                    .FirstOrDefault();
                if (bestSale.Receipt != null)
                {
                    return bestSale;
                }
            }

            return candidates[0];
        }

        return candidates
            .OrderByDescending(x => GetReceiptEv(x.Receipt, x.UseOptional, GetWeatherOutputPercent(domik.Type.Id)) / x.Receipt.PlodderCount)
            .ThenBy(x => x.Receipt.Id)
            .First();
    }

    private bool ShouldUseOptional(int domikTypeId, Receipt receipt)
    {
        return _scenario != ScenarioKind.Casual
            && receipt.OptionalInputResources.Length > 0
            && GetReceiptEv(receipt, true, GetWeatherOutputPercent(domikTypeId)) > GetReceiptEv(receipt, false, GetWeatherOutputPercent(domikTypeId))
            && CanAffordResources(GetInputs(receipt, true));
    }

    private bool StartManufacture(SimDomik domik, Receipt receipt, bool useOptional, IReadOnlyList<SimWorker>? explicitWorkers)
    {
        if (domik.Level == 0 || domik.Manufactures.Count >= GetDomikLevel(domik).MaxManufactureCount || !GetReceipts(domik).Any(x => x.Id == receipt.Id))
        {
            return false;
        }

        var inputs = GetInputs(receipt, useOptional).ToArray();
        if (!CanAffordResources(inputs))
        {
            return false;
        }

        var workers = explicitWorkers?.ToArray() ?? SelectWorkers(domik.Type.Id, receipt.PlodderCount);
        if (workers.Length != receipt.PlodderCount || workers.Any(x => !IsFree(x)))
        {
            return false;
        }

        DeductResources(inputs);
        var duration = CalculateDuration(domik.Type.Id, receipt, useOptional, workers);
        var manufacture = new SimManufacture
        {
            Domik = domik,
            Receipt = receipt,
            Workers = workers,
            UseOptional = useOptional && receipt.OptionalInputResources.Length > 0,
            AutoRepeat = true,
            FinishAt = _now + duration,
            OutputPercent = GetWeatherOutputPercent(domik.Type.Id),
        };
        domik.Manufactures.Add(manufacture);
        foreach (var worker in workers)
        {
            worker.Manufacture = manufacture;
        }

        Schedule(SimulationEventKind.ManufactureFinished, manufacture.FinishAt, manufacture);
        return true;
    }

    private SimWorker[] SelectWorkers(int domikTypeId, int count)
    {
        var workers = GetFreeWorkers();
        if (GetVillageLevel() >= VillageLevelCalculator.SmartAutoUnlockLevel)
        {
            return workers
                .OrderByDescending(x => -x.Trait.DurationPercent + WorkerSkillCalculator.GetBonusPercent(x.Skills.GetValueOrDefault(domikTypeId)))
                .ThenBy(x => x.Id)
                .Take(count)
                .ToArray();
        }

        return workers.OrderBy(x => x.Id).Take(count).ToArray();
    }

    private int CalculateDuration(int domikTypeId, Receipt receipt, bool useOptional, IReadOnlyList<SimWorker> workers)
    {
        var duration = receipt.DurationSeconds;
        if (useOptional && receipt.OptionalInputResources.Length > 0)
        {
            duration = receipt.DurationSeconds * (100 - receipt.SpeedupPercent) / 100;
        }

        var averageTraitSpeedup = workers.Average(x => -x.Trait.DurationPercent);
        duration = (int)Math.Ceiling(duration * (100 - averageTraitSpeedup) / 100);
        var averageSkillSpeedup = workers.Average(x => WorkerSkillCalculator.GetBonusPercent(x.Skills.GetValueOrDefault(domikTypeId)));
        duration = (int)Math.Ceiling(duration * (100 - averageSkillSpeedup) / 100);
        return Math.Max(duration, (int)Math.Ceiling(receipt.DurationSeconds * 0.6));
    }

    private void FinishManufacture(SimManufacture manufacture)
    {
        if (!manufacture.Domik.Manufactures.Remove(manufacture))
        {
            return;
        }

        foreach (var output in manufacture.Receipt.OutputResources)
        {
            AddResource(output.Type.Id, Math.Max(1, (int)Math.Round(output.Value * manufacture.OutputPercent / 100.0)));
        }

        foreach (var worker in manufacture.Workers)
        {
            worker.Skills[manufacture.Domik.Type.Id] = worker.Skills.GetValueOrDefault(manufacture.Domik.Type.Id) + 1;
            if (!worker.Trait.NoFatigue)
            {
                worker.WorkedSeconds += manufacture.Receipt.DurationSeconds;
                if (worker.WorkedSeconds >= DomikManager.FatigueThresholdSeconds)
                {
                    worker.RestUntil = _now + DomikManager.RestSeconds * (100 - Math.Min(DomikManager.RestComfortMaxPercent, 0)) / 100;
                    worker.WorkedSeconds = 0;
                }
            }

            worker.Manufacture = null;
        }

        if (manufacture.AutoRepeat)
        {
            StartManufacture(manufacture.Domik, manufacture.Receipt, manufacture.UseOptional, manufacture.Workers);
        }
    }

    private void FinishDomik(SimDomik domik)
    {
        if (domik.UpgradeFinishAt != _now)
        {
            return;
        }

        domik.UpgradeFinishAt = null;
        domik.Level++;
        if (domik.Level == 1)
        {
            _result.FirstDomikTimes.TryAdd(domik.Type.Id, _now);
        }

        if (domik.Level == domik.Type.MaxLevel)
        {
            _result.MaxDomikLevelTimes.TryAdd(domik.Type.Id, _now);
        }

        EnsureWorkers();
        ObserveVillageLevel();
        CheckContentComplete();
    }

    private bool StartExpeditions()
    {
        var scoutHutLevel = GetScoutHutLevel();
        if (scoutHutLevel == 0)
        {
            return false;
        }

        var started = false;
        var attempts = _scenario == ScenarioKind.Casual ? 1 : scoutHutLevel;
        for (var attempt = 0; attempt < attempts && _state.Expeditions.Count < scoutHutLevel; attempt++)
        {
            if (_scenario == ScenarioKind.Casual && _random.NextDouble() >= 0.5)
            {
                break;
            }

            var expedition = SelectExpedition();
            if (expedition == null || !StartExpedition(expedition))
            {
                break;
            }

            started = true;
        }

        return started;
    }

    private ExpeditionType? SelectExpedition()
    {
        var available = _data.ExpeditionTypes
            .Where(x => GetFreeWorkers().Count >= x.WorkerCount && CanAffordResources(GetExpeditionCost(x)))
            .ToArray();
        if (available.Length == 0)
        {
            return null;
        }

        if (_scenario == ScenarioKind.Casual)
        {
            return available[0];
        }

        return available
            .OrderByDescending(x => GetExpeditionEv(x) / x.WorkerCount / (x.DurationSeconds / 3600.0))
            .ThenBy(x => x.Id)
            .First();
    }

    private bool StartExpedition(ExpeditionType type)
    {
        var workers = _scenario == ScenarioKind.Casual
            ? GetFreeWorkers().OrderBy(x => x.Id).Take(type.WorkerCount).ToArray()
            : GetFreeWorkers().OrderByDescending(x => x.Trait.LuckWeightPercent).ThenBy(x => x.Id).Take(type.WorkerCount).ToArray();
        var cost = GetExpeditionCost(type).ToArray();
        if (workers.Length != type.WorkerCount || !CanAffordResources(cost))
        {
            return false;
        }

        DeductResources(cost);
        var expedition = new SimExpedition
        {
            Id = ++_state.NextExpeditionId,
            Type = type,
            Workers = workers,
            FinishAt = _now + type.DurationSeconds,
        };
        _state.Expeditions.Add(expedition);
        foreach (var worker in workers)
        {
            worker.Expedition = expedition;
        }

        Schedule(SimulationEventKind.ExpeditionReturned, expedition.FinishAt, expedition);
        return true;
    }

    private void FinishExpedition(SimExpedition expedition)
    {
        if (!_state.Expeditions.Remove(expedition))
        {
            return;
        }

        var groupLuck = expedition.Workers.Max(x => x.Trait.LuckWeightPercent);
        var forced = _state.ExpeditionsSincePity >= ExpeditionManager.ExpeditionPityThreshold;
        var gotRare = false;
        for (var roll = 0; roll < expedition.Type.RollCount; roll++)
        {
            var pool = forced && !gotRare
                ? expedition.Type.Loot.Where(x => x.IsRare).ToArray()
                : expedition.Type.Loot;
            if (pool.Length == 0)
            {
                pool = expedition.Type.Loot;
            }

            var loot = PickLoot(pool, groupLuck);
            gotRare |= loot.IsRare;
            AddResource(loot.ResourceTypeId, _random.Next(loot.MinValue, loot.MaxValue + 1));
        }

        _state.ExpeditionsSincePity = gotRare ? 0 : _state.ExpeditionsSincePity + 1;
        foreach (var worker in expedition.Workers)
        {
            if (!worker.Trait.NoFatigue)
            {
                worker.RestUntil = _now + ExpeditionManager.ExpeditionRestSeconds;
            }

            worker.Expedition = null;
        }
    }

    private ExpeditionLoot PickLoot(ExpeditionLoot[] pool, int luckPercent)
    {
        var totalWeight = pool.Sum(x => ExpeditionManager.ScaleWeight(x.IsRare, x.Weight, luckPercent));
        var roll = _random.Next(totalWeight);
        var cumulative = 0;
        foreach (var loot in pool)
        {
            cumulative += ExpeditionManager.ScaleWeight(loot.IsRare, loot.Weight, luckPercent);
            if (roll < cumulative)
            {
                return loot;
            }
        }

        return pool[^1];
    }

    private double GetExpeditionEv(ExpeditionType type)
    {
        var bestLuck = GetFreeWorkers().OrderByDescending(x => x.Trait.LuckWeightPercent).Take(type.WorkerCount).FirstOrDefault()?.Trait.LuckWeightPercent ?? 0;
        var normal = GetLootEv(type.Loot, bestLuck);
        if (_state.ExpeditionsSincePity < ExpeditionManager.ExpeditionPityThreshold)
        {
            return normal * type.RollCount;
        }

        var rare = type.Loot.Where(x => x.IsRare).ToArray();
        return (rare.Length == 0 ? normal : GetLootEv(rare, bestLuck)) + normal * (type.RollCount - 1);
    }

    private static double GetLootEv(ExpeditionLoot[] loot, int luckPercent)
    {
        var totalWeight = loot.Sum(x => ExpeditionManager.ScaleWeight(x.IsRare, x.Weight, luckPercent));
        return loot.Sum(x => ExpeditionManager.ScaleWeight(x.IsRare, x.Weight, luckPercent) / (double)totalWeight
            * ((x.MinValue + x.MaxValue) / 2.0) * ResourceManager.GetMarketValue(x.ResourceTypeId));
    }

    private IEnumerable<Resource> GetExpeditionCost(ExpeditionType type)
    {
        yield return new Resource { Type = _data.ResourceTypeById[5], Value = type.GoldCost };
        foreach (var equipment in type.Equipment)
        {
            yield return new Resource { Type = _data.ResourceTypeById[equipment.ResourceTypeId], Value = equipment.Value };
        }
    }

    private WeatherType PickWeatherType()
    {
        var totalWeight = _data.WeatherTypes.Sum(x => x.RotationWeight);
        var roll = _random.Next(totalWeight);
        var cumulative = 0;
        foreach (var weatherType in _data.WeatherTypes)
        {
            cumulative += weatherType.RotationWeight;
            if (roll < cumulative)
            {
                return weatherType;
            }
        }

        return _data.WeatherTypes[^1];
    }

    private int GetWeatherOutputPercent(int domikTypeId)
    {
        return _data.WeatherTypeById[_state.CurrentWeatherTypeId].Effects
            .FirstOrDefault(x => x.DomikTypeId == domikTypeId)?.OutputPercent ?? 100;
    }

    private IEnumerable<Resource> GetInputs(Receipt receipt, bool useOptional)
    {
        return useOptional && receipt.OptionalInputResources.Length > 0
            ? receipt.InputResources.Concat(receipt.OptionalInputResources)
            : receipt.InputResources;
    }

    private double GetBestReceiptEv(DomikType type, UpgradeLevel level)
    {
        var receipts = level.Receipts.Select(x => _data.ReceiptById[x.Id]).ToArray();
        return receipts.Length == 0 ? 0 : receipts.Max(x => Math.Max(GetReceiptEv(x, false, 100), GetReceiptEv(x, true, 100)));
    }

    private double GetReceiptEv(Receipt receipt, bool useOptional, int outputPercent)
    {
        if (useOptional && receipt.OptionalInputResources.Length == 0)
        {
            useOptional = false;
        }

        var output = receipt.OutputResources.Sum(x => Math.Max(1, (int)Math.Round(x.Value * outputPercent / 100.0)) * ResourceManager.GetMarketValue(x.Type.Id));
        var input = GetInputs(receipt, useOptional).Sum(x => x.Value * ResourceManager.GetMarketValue(x.Type.Id));
        var duration = useOptional
            ? receipt.DurationSeconds * (100 - receipt.SpeedupPercent) / 100
            : receipt.DurationSeconds;
        return (output - input) / (duration / 3600.0);
    }

    private void EnsureWorkers()
    {
        while (_state.Workers.Count < GetCapacity())
        {
            _state.Workers.Add(new SimWorker
            {
                Id = ++_state.NextWorkerId,
                Trait = _data.Traits[_random.Next(_data.Traits.Length)],
            });
        }
    }

    private int GetVillageLevel()
    {
        var buildings = _state.Domiks.Sum(x => x.Level);
        var residents = GetCapacity();
        var reputationMilestones = _state.Reputation.Values.Sum(x => x / VillageLevelCalculator.ReputationPointsPerMilestone);
        return VillageLevelCalculator.ComputeLevel(buildings, residents, reputationMilestones, 0);
    }

    private int GetCapacity()
    {
        return _state.Domiks.Where(x => x.Level > 0).Sum(x => GetCapacity(x.Type, GetDomikLevel(x)));
    }

    private int GetCapacity(DomikType type, UpgradeLevel level)
    {
        return level.Modificators.FirstOrDefault(x => x.Type.Id == _data.PlodderModificatorId)?.Value ?? 0;
    }

    private void ObserveVillageLevel()
    {
        var villageLevel = GetVillageLevel();
        for (var level = 1; level <= villageLevel; level++)
        {
            _result.VillageLevelTimes.TryAdd(level, _now);
        }

        _result.MaxVillageLevel = Math.Max(_result.MaxVillageLevel, villageLevel);
        foreach (var neighbor in _data.Neighbors.Where(x => x.UnlockLevel <= villageLevel))
        {
            _result.NeighborOpenTimes.TryAdd(neighbor.Id, _now);
        }
    }

    private void CheckContentComplete()
    {
        if (_data.DomikTypes.All(type => _state.Domiks.Count(x => x.Type.Id == type.Id) == type.MaxCount
            && _state.Domiks.Where(x => x.Type.Id == type.Id).All(x => x.Level == type.MaxLevel)))
        {
            _result.ContentCompleteTime = _now;
            _finished = true;
        }
    }

    private void RecordAction()
    {
        if (_now - _lastActionTime > StallWarningSeconds)
        {
            _result.LongestStallSeconds = Math.Max(_result.LongestStallSeconds, _now - _lastActionTime);
        }

        _lastActionTime = _now;
    }

    private void FinishResult()
    {
        if (_result.ContentCompleteTime == null && _now - _lastActionTime > StallWarningSeconds)
        {
            _result.LongestStallSeconds = Math.Max(_result.LongestStallSeconds, _now - _lastActionTime);
        }

        foreach (var resourceType in _data.ResourceTypes)
        {
            _result.FinalResources[resourceType.Id] = GetResource(resourceType.Id);
        }

        if (_state.TotalWorkerSeconds > 0)
        {
            _result.IdleShare = _state.FreeWorkerSeconds / (double)_state.TotalWorkerSeconds;
            _result.RestShare = _state.RestWorkerSeconds / (double)_state.TotalWorkerSeconds;
        }
    }

    private bool RequiresBlueprint(DomikType type)
    {
        return _data.Blueprints.Any(x => x.DomikTypeId == type.Id);
    }

    private bool HasBlueprint(DomikType type)
    {
        return _data.Blueprints.Where(x => x.DomikTypeId == type.Id).All(x => _state.OwnedBlueprints.Contains(x.Id));
    }

    private UpgradeLevel GetDomikLevel(SimDomik domik)
    {
        return domik.Type.Levels.Single(x => x.Value == domik.Level);
    }

    private Receipt[] GetReceipts(SimDomik domik)
    {
        return GetDomikLevel(domik).Receipts.Select(x => _data.ReceiptById[x.Id]).ToArray();
    }

    private int GetScoutHutLevel()
    {
        return _state.Domiks.Where(x => x.Type.LogicName == "scout_hut").Select(x => x.Level).DefaultIfEmpty().Max();
    }

    private List<SimWorker> GetFreeWorkers()
    {
        return _state.Workers.Where(IsFree).OrderBy(x => x.Id).ToList();
    }

    private bool IsFree(SimWorker worker)
    {
        return !worker.IsBusy && (worker.RestUntil == null || worker.RestUntil <= _now);
    }

    private int GetReputation(int neighborId)
    {
        return _state.Reputation.GetValueOrDefault(neighborId);
    }

    private int GetResource(int resourceTypeId)
    {
        return _state.Resources.GetValueOrDefault(resourceTypeId);
    }

    private bool CanAfford(int resourceTypeId, int value)
    {
        return GetResource(resourceTypeId) >= value;
    }

    private bool CanAffordResources(IEnumerable<Resource> resources)
    {
        return resources.GroupBy(x => x.Type.Id).All(x => GetResource(x.Key) >= x.Sum(y => y.Value));
    }

    private void DeductResources(IEnumerable<Resource> resources)
    {
        foreach (var group in resources.GroupBy(x => x.Type.Id))
        {
            RemoveResource(group.Key, group.Sum(x => x.Value));
        }
    }

    private void AddResource(int resourceTypeId, int value)
    {
        _state.Resources[resourceTypeId] = GetResource(resourceTypeId) + value;
    }

    private void RemoveResource(int resourceTypeId, int value)
    {
        _state.Resources[resourceTypeId] = GetResource(resourceTypeId) - value;
    }

    private sealed class SimulationState
    {
        public Dictionary<int, int> Resources { get; } = [];
        public Dictionary<int, int> Reputation { get; } = [];
        public HashSet<int> OwnedBlueprints { get; } = [];
        public List<SimDomik> Domiks { get; } = [];
        public List<SimWorker> Workers { get; } = [];
        public List<SimOrder> Orders { get; } = [];
        public List<SimExpedition> Expeditions { get; } = [];
        public int CurrentWeatherTypeId { get; set; }
        public int ExpeditionsSincePity { get; set; }
        public int NextDomikId { get; set; }
        public int NextWorkerId { get; set; }
        public int NextOrderId { get; set; }
        public int NextExpeditionId { get; set; }
        public long TotalWorkerSeconds { get; set; }
        public long FreeWorkerSeconds { get; set; }
        public long RestWorkerSeconds { get; set; }
    }

    private sealed class SimDomik
    {
        public required int Id { get; init; }
        public required DomikType Type { get; init; }
        public int Level { get; set; }
        public int? UpgradeFinishAt { get; set; }
        public List<SimManufacture> Manufactures { get; } = [];
    }

    private sealed class SimWorker
    {
        public required int Id { get; init; }
        public required Trait Trait { get; init; }
        public Dictionary<int, int> Skills { get; } = [];
        public int WorkedSeconds { get; set; }
        public int? RestUntil { get; set; }
        public SimManufacture? Manufacture { get; set; }
        public SimExpedition? Expedition { get; set; }
        public bool IsBusy => Manufacture != null || Expedition != null;
    }

    private sealed class SimManufacture
    {
        public required SimDomik Domik { get; init; }
        public required Receipt Receipt { get; init; }
        public required SimWorker[] Workers { get; init; }
        public required bool UseOptional { get; init; }
        public required bool AutoRepeat { get; init; }
        public required int FinishAt { get; init; }
        public required int OutputPercent { get; init; }
    }

    private sealed class SimOrder
    {
        public required int Id { get; init; }
        public required Neighbor Neighbor { get; init; }
        public required int ResourceTypeId { get; init; }
        public required int Quantity { get; init; }
        public required int RewardCoins { get; init; }
        public required int RewardGold { get; init; }
        public required int RewardReputation { get; init; }
        public required int ExpireAt { get; init; }
    }

    private sealed class SimExpedition
    {
        public required int Id { get; init; }
        public required ExpeditionType Type { get; init; }
        public required SimWorker[] Workers { get; init; }
        public required int FinishAt { get; init; }
    }

    private sealed class DomikCandidate
    {
        public DomikCandidate(DomikType domikType, SimDomik? domik, UpgradeLevel level)
        {
            Type = domikType;
            Domik = domik;
            Level = level;
        }

        public DomikType Type { get; }
        public SimDomik? Domik { get; }
        public UpgradeLevel Level { get; }
    }

    private sealed class SimulationEvent
    {
        public required SimulationEventKind Kind { get; init; }
        public required int Time { get; init; }
        public object? Target { get; init; }
    }

    private readonly record struct EventPriority(int Time, int Rank, long Sequence) : IComparable<EventPriority>
    {
        public int CompareTo(EventPriority other)
        {
            var time = Time.CompareTo(other.Time);
            if (time != 0)
            {
                return time;
            }

            var rank = Rank.CompareTo(other.Rank);
            return rank != 0 ? rank : Sequence.CompareTo(other.Sequence);
        }
    }
}

public sealed class BalanceReport
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly SimulationData _data;
    private readonly SimulationReport _report;

    public BalanceReport(SimulationData data, SimulationReport report)
    {
        _data = data;
        _report = report;
    }

    public string Render()
    {
        var output = new StringBuilder();
        output.AppendLine($"Баланс-симулятор: домиков { _data.DomikTypes.Length }, рецептов { _data.Receipts.Length }, соседей { _data.Neighbors.Length }, экспедиций { _data.ExpeditionTypes.Length }");
        output.AppendLine("Горизонт: 45 суток, по 7 прогонов на сценарий, сиды 1–7.");
        foreach (var scenario in new[] { ScenarioKind.Casual, ScenarioKind.Optimal, ScenarioKind.Extreme })
        {
            RenderScenario(output, scenario);
        }

        RenderDiagnostics(output);
        return output.ToString().TrimEnd();
    }

    private void RenderScenario(StringBuilder output, ScenarioKind scenario)
    {
        var runs = _report.Runs[scenario];
        output.AppendLine();
        output.AppendLine($"Сценарий: {GetScenarioName(scenario)}");
        output.AppendLine("Обжитость");
        output.AppendLine("  Уровень  Время, ч");
        var maxLevel = MedianInt(runs.Select(x => x.MaxVillageLevel));
        for (var level = 1; level <= maxLevel; level++)
        {
            output.AppendLine($"  {level,7}  {FormatTime(MedianTime(runs.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1))))}");
        }

        output.AppendLine("Постройки");
        output.AppendLine("  Тип                         Первая, ч  Макс. уровень, ч");
        foreach (var type in _data.DomikTypes)
        {
            var first = MedianTime(runs.Select(x => x.FirstDomikTimes.GetValueOrDefault(type.Id, -1)));
            var maximum = MedianTime(runs.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            output.AppendLine($"  {type.Name.PadRight(26)}  {FormatTime(first).PadLeft(9)}  {FormatTime(maximum).PadLeft(16)}");
        }

        output.AppendLine("Соседи и чертежи");
        output.AppendLine("  Веха                        Время, ч");
        foreach (var neighbor in _data.Neighbors)
        {
            var time = MedianTime(runs.Select(x => x.NeighborOpenTimes.GetValueOrDefault(neighbor.Id, -1)));
            output.AppendLine($"  {("Сосед " + neighbor.Name).PadRight(26)}  {FormatTime(time).PadLeft(9)}");
        }

        foreach (var blueprint in _data.Blueprints)
        {
            var time = MedianTime(runs.Select(x => x.BlueprintTimes.GetValueOrDefault(blueprint.Id, -1)));
            output.AppendLine($"  {("Чертёж " + blueprint.Name).PadRight(26)}  {FormatTime(time).PadLeft(9)}");
        }

        output.AppendLine($"Трудяги: простой {FormatPercent(Median(runs.Select(x => x.IdleShare)))}; отдых {FormatPercent(Median(runs.Select(x => x.RestShare)))}.");
        output.AppendLine("Финальные стоки");
        output.AppendLine("  Ресурс                      Кол-во  Монетный эквивалент");
        foreach (var resourceType in _data.ResourceTypes)
        {
            var value = MedianInt(runs.Select(x => x.FinalResources.GetValueOrDefault(resourceType.Id)));
            var coins = value * ResourceManager.GetMarketValue(resourceType.Id);
            output.AppendLine($"  {resourceType.Name.PadRight(26)}  {value,6}  {coins,20}");
        }

        var total = MedianInt(runs.Select(x => x.FinalResources.Sum(resource => resource.Value * ResourceManager.GetMarketValue(resource.Key))));
        output.AppendLine($"  {"Итого".PadRight(26)}  {string.Empty,6}  {total,20}");
        output.AppendLine($"Весь контент выкачан: {FormatTime(MedianTime(runs.Select(x => x.ContentCompleteTime ?? -1)))}.");
    }

    private void RenderDiagnostics(StringBuilder output)
    {
        var casual = _report.Runs[ScenarioKind.Casual];
        var extreme = _report.Runs[ScenarioKind.Extreme];
        output.AppendLine();
        output.AppendLine("Диагностика §8.7");
        output.AppendLine("  Веха                                      Экстремал/казуал");
        foreach (var level in new[] { 8, 20 })
        {
            var casualTime = MedianTime(casual.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1)));
            var extremeTime = MedianTime(extreme.Select(x => x.VillageLevelTimes.GetValueOrDefault(level, -1)));
            output.AppendLine($"  {("Обжитость " + level).PadRight(40)}  {FormatRatio(extremeTime, casualTime)}");
        }

        foreach (var type in _data.DomikTypes.Where(x => x.MaxLevel > 1))
        {
            var casualTime = MedianTime(casual.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            var extremeTime = MedianTime(extreme.Select(x => x.MaxDomikLevelTimes.GetValueOrDefault(type.Id, -1)));
            output.AppendLine($"  {(type.Name + ", макс. уровень").PadRight(40)}  {FormatRatio(extremeTime, casualTime)}");
        }

        var stalled = casual.Where(x => x.LongestStallSeconds > 24 * 60 * 60).ToArray();
        if (stalled.Length == 0)
        {
            output.AppendLine("  Казуальный: достижимых действий не терял более чем на 24 ч.");
            return;
        }

        var longest = stalled.Max(x => x.LongestStallSeconds);
        output.AppendLine($"  ПРЕДУПРЕЖДЕНИЕ: казуальный стоял более 24 ч; максимум {FormatHours(longest)} ч, сиды {string.Join(", ", stalled.Select(x => x.Seed))}.");
    }

    private static string GetScenarioName(ScenarioKind scenario)
    {
        return scenario switch
        {
            ScenarioKind.Casual => "Казуальный",
            ScenarioKind.Optimal => "Оптимальный",
            ScenarioKind.Extreme => "Экстремальный",
            _ => string.Empty,
        };
    }

    private static int? MedianTime(IEnumerable<int> values)
    {
        var value = values.Select(x => x < 0 ? int.MaxValue : x).OrderBy(x => x).ElementAt(3);
        return value == int.MaxValue ? null : value;
    }

    private static int MedianInt(IEnumerable<int> values)
    {
        return values.OrderBy(x => x).ElementAt(3);
    }

    private static double Median(IEnumerable<double> values)
    {
        return values.OrderBy(x => x).ElementAt(3);
    }

    private static string FormatTime(int? seconds)
    {
        return seconds == null ? "не достигнут" : FormatHours(seconds.Value);
    }

    private static string FormatHours(int seconds)
    {
        return (seconds / 3600.0).ToString("F1", RussianCulture);
    }

    private static string FormatPercent(double value)
    {
        return (value * 100).ToString("F1", RussianCulture) + "%";
    }

    private static string FormatRatio(int? numerator, int? denominator)
    {
        if (numerator == null || denominator == null || denominator == 0)
        {
            return "не достигнут";
        }

        return (numerator.Value / (double)denominator.Value).ToString("F2", RussianCulture) + "×";
    }
}
