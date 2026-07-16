using Domiki.Web.Activities.Models;
using Domiki.Web.Data;
using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Reference;
using Domiki.Web.Village;
using Receipt = Domiki.Web.Reference.Models.Receipt;

namespace Domiki.Web.Activities;

public class GoalManager
{
    private readonly ApplicationDbContext _context;
    private readonly ResourceManager _resourceManager;
    private readonly PlayerResourceManager _playerResourceManager;
    private readonly VillageLevelCalculator _villageLevelCalculator;
    private readonly PlayerEventManager _playerEventManager;

    public GoalManager(ApplicationDbContext context, ResourceManager resourceManager, PlayerResourceManager playerResourceManager, VillageLevelCalculator villageLevelCalculator, PlayerEventManager playerEventManager)
    {
        _context = context;
        _resourceManager = resourceManager;
        _playerResourceManager = playerResourceManager;
        _villageLevelCalculator = villageLevelCalculator;
        _playerEventManager = playerEventManager;
    }

    public GoalsState GetGoalsState(int playerId)
    {
        _playerResourceManager.LockDbPlayerRow(playerId);
        Advance(playerId, null);

        var goals = _resourceManager.GetStarterGoals();
        var completedGoalIds = _context.PlayerGoals
            .Where(x => x.PlayerId == playerId && goals.Select(g => g.Id).Contains(x.GoalId))
            .Select(x => x.GoalId)
            .ToHashSet();

        var activeGoal = goals.FirstOrDefault(x => !completedGoalIds.Contains(x.Id));
        var zealCharges = _context.Players.Where(x => x.Id == playerId).Select(x => x.ZealCharges).Single();

        return new()
        {
            ActiveGoal = activeGoal == null
                ? null
                : new ActiveGoal
                {
                    Id = activeGoal.Id,
                    Ordinal = activeGoal.Ordinal,
                    Name = activeGoal.Name,
                    RewardCoins = activeGoal.RewardCoins,
                },
            CompletedCount = completedGoalIds.Count,
            TotalCount = goals.Length,
            ZealCharges = zealCharges,
        };
    }

    public void OnManufactureStarted(int playerId, Receipt receipt)
    {
        Advance(playerId, goal => goal.ConditionType switch
        {
            GoalConditionType.StartAnyManufacture => receipt.DurationSeconds >= goal.Param,
            GoalConditionType.SellAnyResource => receipt.LogicName?.StartsWith("sell_") == true,
            _ => false,
        });
    }

    public void OnOrderCompleted(int playerId)
    {
        Advance(playerId, goal => goal.ConditionType == GoalConditionType.CompleteAnyOrder);
    }

    private void Advance(int playerId, Func<StarterGoal, bool> actionMatch)
    {
        var goals = _resourceManager.GetStarterGoals();
        var completedGoalIds = _context.PlayerGoals
            .Where(x => x.PlayerId == playerId && goals.Select(g => g.Id).Contains(x.GoalId))
            .Select(x => x.GoalId)
            .ToHashSet();

        var actionAvailable = actionMatch != null;
        var completedAny = false;

        while (true)
        {
            var goal = goals.FirstOrDefault(x => !completedGoalIds.Contains(x.Id));
            if (goal == null)
            {
                break;
            }

            var actionCompleted = actionAvailable && actionMatch(goal);
            if (!actionCompleted && !IsStateConditionMet(playerId, goal))
            {
                break;
            }

            _context.PlayerGoals.Add(new()
            {
                PlayerId = playerId,
                GoalId = goal.Id,
                CompleteDate = DateTimeHelper.GetNowDate(),
            });

            _playerResourceManager.GrantResource(playerId, 1, goal.RewardCoins);
            _playerEventManager.Record(playerId, PlayerEventType.GoalCompleted, new { goalId = goal.Id, name = goal.Name, rewardCoins = goal.RewardCoins });
            completedGoalIds.Add(goal.Id);
            if (actionCompleted)
            {
                actionAvailable = false;
            }

            completedAny = true;
        }

        if (completedAny)
        {
            _context.SaveChanges();
        }
    }

    private bool IsStateConditionMet(int playerId, StarterGoal goal)
    {
        return goal.ConditionType switch
        {
            GoalConditionType.BuildDomikType => _context.Domiks.Any(x => x.PlayerId == playerId && x.TypeId == goal.Param),
            GoalConditionType.UpgradeDomikToLevel => _context.Domiks.Any(x => x.PlayerId == playerId && x.Level >= goal.Param && (goal.Param2 == 0 || x.TypeId == goal.Param2)),
            GoalConditionType.ReachVillageLevel => _villageLevelCalculator.GetLevel(playerId).Level >= goal.Param,
            _ => false,
        };
    }
}
