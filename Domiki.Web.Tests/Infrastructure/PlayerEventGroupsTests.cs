using Domiki.Web.Infrastructure;
using PlayerEventGroup = Domiki.Web.Data.Entities.PlayerEventGroup;
using PlayerEventType = Domiki.Web.Data.Entities.PlayerEventType;

namespace Domiki.Web.Tests;

public sealed class PlayerEventGroupsTests
{
    /// <summary>
    /// Каждый тип события отнесён к группе: непокрытый тип выпал бы из любого фильтра журнала молча.
    /// </summary>
    [Test]
    public void EveryEventTypeBelongsToAGroupTest()
    {
        var ungrouped = Enum.GetValues<PlayerEventType>()
            .Where(x => x != PlayerEventType.None)
            .Where(x => PlayerEventGroups.Of(x) == PlayerEventGroup.None)
            .ToArray();

        Assert.That(ungrouped, Is.Empty, "типы без группы: " + string.Join(", ", ungrouped));
    }

    /// <summary>
    /// В каждой группе есть хотя бы один тип: пустая группа означала бы фильтр, который всегда даёт пустую выдачу.
    /// </summary>
    [Test]
    public void EveryGroupHasAtLeastOneTypeTest()
    {
        var empty = Enum.GetValues<PlayerEventGroup>()
            .Where(x => x != PlayerEventGroup.None)
            .Where(x => PlayerEventGroups.TypesOf(x).Length == 0)
            .ToArray();

        Assert.That(empty, Is.Empty, "пустые группы: " + string.Join(", ", empty));
    }
}
