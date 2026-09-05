namespace Domiki.BalanceSim;

public sealed class SimulationRunResult
{
    public required int Seed { get; init; }
    public required Dictionary<int, int> VillageLevelTimes { get; init; }
    public required Dictionary<int, int> FirstDomikTimes { get; init; }
    public required Dictionary<int, int> MaxDomikLevelTimes { get; init; }
    public required Dictionary<int, int> NeighborOpenTimes { get; init; }
    public required Dictionary<int, int> BlueprintTimes { get; init; }
    public required Dictionary<int, int> FinalResources { get; init; }

    /// <summary>
    /// Сколько раз за прогон запускался каждый рецепт: ключ – идентификатор рецепта, значение – число стартов.
    /// </summary>
    public required Dictionary<int, int> ReceiptStartCounts { get; init; }
    public int? ContentCompleteTime { get; set; }
    public int MaxVillageLevel { get; set; }
    public double IdleShare { get; set; }
    public double RestShare { get; set; }
    public int LongestStallSeconds { get; set; }
    public int? FirstIncomeSeconds { get; set; }
    public int? FirstUpgradeStartSeconds { get; set; }
    public int ActionsFirst15Min { get; set; }
    public int InfeasibleOrdersDay1 { get; set; }
    public int GoalsCompleted48h { get; set; }
    public long TotalWorkerSeconds { get; set; }
    public int ManufactureStartCount { get; set; }
    public int ClampFireCount { get; set; }

    /// <summary>
    /// Число Артельных изб на конец прогона.
    /// </summary>
    public int BarracksCount { get; set; }

    /// <summary>
    /// Сумма уровней Артельных изб на конец прогона.
    /// </summary>
    public int BarracksLevelSum { get; set; }

    /// <summary>
    /// Число коек всех построек на конец прогона – до ограничения капом <see cref="Domiki.Web.Workers.WorkerManager.MaxCapacity"/>.
    /// </summary>
    public int BedCount { get; set; }

    /// <summary>
    /// Вместимость артели на конец прогона – койки после ограничения капом.
    /// </summary>
    public int WorkerCapacity { get; set; }

    /// <summary>
    /// Число трудяг на конец прогона.
    /// </summary>
    public int WorkerCount { get; set; }

    /// <summary>
    /// Секунда прогона, на которой койки впервые достигли капа; <c>null</c>, если кап так и не сработал.
    /// </summary>
    public int? WorkerCapReachedSeconds { get; set; }
}
