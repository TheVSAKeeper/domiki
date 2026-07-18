using Domiki.Web.Activities;
using Domiki.Web.Activities.Models;
using Domiki.Web.Infrastructure;
using Domiki.Web.Village;
using Domiki.Web.Village.Models;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class TolokaArtifactTests
{
    private const int GranaryTolokaTypeId = 2;

    private static readonly List<int> CreatedTolokaIds = [];

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (CreatedTolokaIds.Count == 0)
        {
            return;
        }

        using var scope = App.Scope();
        scope.Context.TolokaContributions.RemoveRange(scope.Context.TolokaContributions.Where(x => CreatedTolokaIds.Contains(x.TolokaId)));
        scope.Context.Tolokas.RemoveRange(scope.Context.Tolokas.Where(x => CreatedTolokaIds.Contains(x.Id)));
        scope.Commit();
    }

    /// <summary>
    /// Завершённая толока попадает в летопись мира с названием своего типа, названием ресурса, числом участников
    /// и сезоном, вычисленным от даты завершения.
    /// </summary>
    [Test]
    public void GetArtifactsReturnsCompletedTolokaWithNameParticipantsAndSeasonTest()
    {
        const int goal = 501234;
        var completedDate = DateTimeHelper.GetNowDate().AddDays(-3);
        var expectedSeason = App.Act<SeasonManager, Season>(m => m.GetCurrentSeason(completedDate)).Number;

        var first = TestPlayer.Create();
        var second = TestPlayer.Create();
        InsertCompletedToloka(GranaryTolokaTypeId, goal, completedDate, first.Id, second.Id);

        var artifact = first.Artifacts().Single(x => x.Goal == goal);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(artifact.Name, Is.EqualTo("Общий амбар"));
            Assert.That(artifact.ResourceName, Is.EqualTo("Дерево"));
            Assert.That(artifact.Participants, Is.EqualTo(2));
            Assert.That(artifact.SeasonNumber, Is.EqualTo(expectedSeason));
            Assert.That(artifact.CompletedDate, Is.EqualTo(completedDate));
        }
    }

    /// <summary>
    /// Активная (незавершённая) толока в летопись мира не попадает.
    /// </summary>
    [Test]
    public void GetArtifactsExcludesActiveTolokaTest()
    {
        const int sentinelGoal = 909090;
        var player = TestPlayer.Create();
        var previousGoal = GetActiveTolokaGoal();

        try
        {
            SetActiveTolokaGoal(sentinelGoal);

            var artifacts = player.Artifacts();

            Assert.That(artifacts.Any(x => x.Goal == sentinelGoal), Is.False);
        }
        finally
        {
            SetActiveTolokaGoal(previousGoal);
        }
    }

    /// <summary>
    /// Летопись мира сортирует толоки по убыванию даты завершения и не длиннее <see cref="TolokaManager.TolokaArtifactShowCount"/>
    /// записей.
    /// </summary>
    [Test]
    public void GetArtifactsOrdersByCompletedDateDescendingAndLimitsToShowCountTest()
    {
        const int goal = 602345;
        var completedDate = DateTimeHelper.GetNowDate().AddYears(50);
        var player = TestPlayer.Create();
        InsertCompletedToloka(GranaryTolokaTypeId, goal, completedDate, player.Id);

        var artifacts = player.Artifacts();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(artifacts.First().Goal, Is.EqualTo(goal));
            Assert.That(artifacts.Length, Is.LessThanOrEqualTo(TolokaManager.TolokaArtifactShowCount));
        }
    }

    private static void InsertCompletedToloka(int tolokaTypeId, int goal, DateTime completedDate, params int[] participantPlayerIds)
    {
        int tolokaId;
        using (var scope = App.Scope())
        {
            var entry = scope.Context.Tolokas.Add(new()
            {
                TolokaTypeId = tolokaTypeId,
                Collected = goal,
                Goal = goal,
                StartDate = completedDate.AddHours(-1),
                CompletedDate = completedDate,
            });

            scope.Commit();
            tolokaId = entry.Entity.Id;
        }

        CreatedTolokaIds.Add(tolokaId);

        using var contributionScope = App.Scope();
        foreach (var playerId in participantPlayerIds)
        {
            contributionScope.Context.TolokaContributions.Add(new()
            {
                TolokaId = tolokaId,
                PlayerId = playerId,
                Value = 1,
            });
        }

        contributionScope.Commit();
    }

    private static int GetActiveTolokaGoal()
    {
        return App.Read(context => context.Tolokas.Single(x => x.CompletedDate == null).Goal);
    }

    private static void SetActiveTolokaGoal(int goal)
    {
        using var scope = App.Scope();
        scope.Context.Tolokas.Single(x => x.CompletedDate == null).Goal = goal;
        scope.Commit();
    }
}

file static class TolokaArtifactTestsActs
{
    public static TolokaArtifact[] Artifacts(this TestPlayer p)
    {
        return App.Act<WorldManager, World>(m => m.GetWorld(p.Id)).TolokaArtifacts;
    }
}
