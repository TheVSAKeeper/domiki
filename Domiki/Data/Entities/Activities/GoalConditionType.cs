namespace Domiki.Web.Data.Entities;

/// <summary>
/// Условие выполнения наказа старосты (StarterGoal.ConditionType) – какое игровое действие или состояние закрывает наказ.
/// </summary>
public enum GoalConditionType
{
    /// <summary>
    /// Значение не задано.
    /// </summary>
    None = 0,

    /// <summary>
    /// У игрока построена постройка типа <see cref="StarterGoal.Param"/>.
    /// </summary>
    /// <remarks>
    /// Проверяется состоянием, см. <see cref="Activities.GoalManager.IsStateConditionMet"/>.
    /// </remarks>
    BuildDomikType = 1,

    /// <summary>
    /// Игрок запустил производство по рецепту длительностью не меньше <see cref="StarterGoal.Param"/> секунд.
    /// </summary>
    /// <remarks>
    /// Проверяется действием, см. <see cref="Activities.GoalManager.OnManufactureStarted"/>.
    /// </remarks>
    StartAnyManufacture = 2,

    /// <summary>
    /// Игрок продал любой ресурс на рынке.
    /// </summary>
    /// <remarks>
    /// Проверяется действием, см. <see cref="Activities.GoalManager.OnManufactureStarted"/> по рецепту с <see cref="Receipt.LogicName"/>, начинающимся на <c>sell_</c>.
    /// </remarks>
    SellAnyResource = 3,

    /// <summary>
    /// У игрока есть домик уровня не ниже <see cref="StarterGoal.Param"/>.
    /// </summary>
    /// <remarks>
    /// Тип домика – <see cref="StarterGoal.Param2"/> (<c>0</c> = любой); проверяется состоянием.
    /// </remarks>
    UpgradeDomikToLevel = 4,

    /// <summary>
    /// Игрок сдал заказ соседу (проверяется действием, <see cref="Activities.GoalManager.OnOrderCompleted"/>).
    /// </summary>
    CompleteAnyOrder = 5,

    /// <summary>
    /// Обжитость деревни игрока достигла <see cref="StarterGoal.Param"/> (проверяется состоянием).
    /// </summary>
    ReachVillageLevel = 6,
}
