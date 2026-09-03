using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class GameCommandBatchTests
{
    private const int GrantedResourceValue = 100000;

    private const int UnknownDomikTypeId = 999999;

    /// <summary>
    /// Пачка команд применяется целиком, а в ответе приходит состояние, в котором уже виден их результат.
    /// </summary>
    [Test]
    public async Task BatchAppliesEveryCommandAndReturnsStateTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var first = CreateUpgradableDomik(playerId);
        var second = CreateUpgradableDomik(playerId);

        using var answer = await Apply(client, Upgrade(first), Upgrade(second));
        var results = answer.RootElement.GetProperty("results");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Statuses(results), Is.EqualTo(new[] { "Applied", "Applied" }));
            Assert.That(answer.RootElement.GetProperty("state").GetProperty("playerId").GetInt32(), Is.EqualTo(playerId));
        }
    }

    /// <summary>
    /// Отвергнутая команда не роняет пачку: соседние применяются, а её отказ приходит текстом для игрока.
    /// </summary>
    [Test]
    public async Task RejectedCommandLeavesNeighboursAppliedTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var domikCount = DomikCount(playerId);

        using var answer = await Apply(client, Buy(UnknownDomikTypeId), Upgrade(domikId));
        var results = answer.RootElement.GetProperty("results");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Statuses(results), Is.EqualTo(new[] { "Rejected", "Applied" }));
            Assert.That(results[0].GetProperty("error").GetString(), Is.Not.Empty);
            Assert.That(DomikLevel(playerId, domikId), Is.EqualTo(2));
            Assert.That(DomikCount(playerId), Is.EqualTo(domikCount));
        }
    }

    /// <summary>
    /// Два улучшения в одной пачке доходят до планировщика оба: отложенные действия копятся списком, а не затирают друг друга.
    /// </summary>
    [Test]
    public async Task BatchSchedulesEventOfEveryCommandTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var first = CreateUpgradableDomik(playerId);
        var second = CreateUpgradableDomik(playerId);

        using var answer = await Apply(client, Upgrade(first), Upgrade(second));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(DomikLevel(playerId, first), Is.EqualTo(2));
            Assert.That(DomikLevel(playerId, second), Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Повторно доехавшая пачка применяется один раз: команды с прежними идентификаторами возвращаются повторами.
    /// </summary>
    [Test]
    public async Task RepeatedBatchAppliesOnceTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var command = Upgrade(domikId);

        using var first = await Apply(client, command);
        var afterFirst = ResourceTotal(playerId);
        using var second = await Apply(client, command);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Statuses(first.RootElement.GetProperty("results")), Is.EqualTo(new[] { "Applied" }));
            Assert.That(Statuses(second.RootElement.GetProperty("results")), Is.EqualTo(new[] { "Duplicate" }));
            Assert.That(ResourceTotal(playerId), Is.EqualTo(afterFirst));
        }
    }

    /// <summary>
    /// Команда со ссылкой на исчезнувший объект отвергается одна: соседняя в той же пачке остаётся применённой.
    /// </summary>
    [Test]
    public async Task CommandOnMissingObjectIsRejectedAloneTest()
    {
        const int missingDomikId = 999999;

        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);

        using var answer = await Apply(client, Upgrade(missingDomikId), Upgrade(domikId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Statuses(answer.RootElement.GetProperty("results")), Is.EqualTo(new[] { "Rejected", "Applied" }));
            Assert.That(DomikLevel(playerId, domikId), Is.EqualTo(2));
        }
    }

    /// <summary>
    /// Наряд без поля autoRepeat отвергается, а не трактуется как снятие наряда.
    /// </summary>
    [Test]
    public async Task AutoRepeatCommandWithoutFlagIsRejectedTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        await ReadPlayerId(client);

        using var answer = await Apply(client, new Command(Guid.NewGuid(), "SetManufactureAutoRepeat", new { manufactureId = 1 }));

        Assert.That(Statuses(answer.RootElement.GetProperty("results")), Is.EqualTo(new[] { "Rejected" }));
    }

    /// <summary>
    /// Команда неизвестного вида отвергается как обычный отказ, а не роняет весь запрос.
    /// </summary>
    [Test]
    public async Task UnknownCommandKindIsRejectedTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        await ReadPlayerId(client);

        using var answer = await Apply(client, new Command(Guid.NewGuid(), "SellTheVillage", new { domikId = 1 }));

        Assert.That(Statuses(answer.RootElement.GetProperty("results")), Is.EqualTo(new[] { "Rejected" }));
    }

    /// <summary>
    /// Пачка длиннее потолка отбивается целиком: длинная транзакция под локом строки игрока стоит дорого всем его запросам.
    /// </summary>
    [Test]
    public async Task BatchLongerThanLimitIsRefusedTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var commands = Enumerable.Range(0, GameCommandBatch.MaxCommands + 1).Select(_ => Upgrade(domikId)).ToArray();

        var response = await Send(client, commands);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed record Command(Guid CommandId, string Kind, object Args);

    private static Command Upgrade(int domikId) => new(Guid.NewGuid(), "UpgradeDomik", new { domikId });

    private static Command Buy(int typeId) => new(Guid.NewGuid(), "BuyDomik", new { typeId });

    private static string[] Statuses(JsonElement results) =>
        results.EnumerateArray().Select(x => x.GetProperty("status").GetString() ?? string.Empty).ToArray();

    private static async Task<JsonDocument> Apply(HttpClient client, params Command[] commands)
    {
        var response = await Send(client, commands);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, Command[] commands) =>
        client.PostAsJsonAsync("/Domiki/ApplyCommands", new { commands });

    private static int DomikLevel(int playerId, int domikId) =>
        App.Read(context => context.Domiks.Single(x => x.PlayerId == playerId && x.Id == domikId).Level);

    private static int DomikCount(int playerId) =>
        App.Read(context => context.Domiks.Count(x => x.PlayerId == playerId));

    private static long ResourceTotal(int playerId) =>
        App.Read(context => context.Resources.Where(x => x.PlayerId == playerId).Sum(x => (long)x.Value));

    private static async Task<int> ReadPlayerId(HttpClient client)
    {
        var response = await client.GetAsync("/Domiki/GetGameState");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var state = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return state.RootElement.GetProperty("playerId").GetInt32();
    }

    private static int CreateUpgradableDomik(int playerId)
    {
        using var scope = App.Scope();
        var nextId = (scope.Context.Domiks.Where(x => x.PlayerId == playerId).Max(x => (int?)x.Id) ?? 0) + 1;
        scope.Context.Domiks.Add(new()
        {
            PlayerId = playerId,
            Id = nextId,
            TypeId = DomikIds.ClayMine,
            Level = 1,
        });

        scope.Commit();
        return nextId;
    }

    private static void GrantResources(int playerId)
    {
        using var scope = App.Scope();
        var typeIds = scope.Context.ResourceTypes.Select(x => x.Id).ToArray();
        foreach (var typeId in typeIds)
        {
            var resource = scope.Context.Resources.FirstOrDefault(x => x.PlayerId == playerId && x.TypeId == typeId);
            if (resource == null)
            {
                scope.Context.Resources.Add(new() { PlayerId = playerId, TypeId = typeId, Value = GrantedResourceValue });
            }
            else if (resource.Value < GrantedResourceValue)
            {
                resource.Value = GrantedResourceValue;
            }
        }

        scope.Commit();
    }
}
