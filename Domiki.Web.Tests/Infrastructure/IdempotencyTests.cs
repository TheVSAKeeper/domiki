using System.Net;
using System.Text.Json;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class IdempotencyTests
{
    private const int GrantedResourceValue = 100000;

    private const int UnknownDomikTypeId = 999999;

    /// <summary>
    /// Повторная отправка команды с тем же идентификатором не выполняет действие второй раз: ресурсы списываются один раз.
    /// </summary>
    [Test]
    public async Task RepeatedCommandUpgradesDomikOnceTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var commandId = Guid.NewGuid();

        var first = await Upgrade(client, domikId, commandId);
        var afterFirst = await ReadResourceTotal(client);
        var second = await Upgrade(client, domikId, commandId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await ReadResourceTotal(client), Is.EqualTo(afterFirst));
        }
    }

    /// <summary>
    /// Идентификатор команды, отбитой бизнес-ошибкой, освобождается вместе с откатом транзакции: тот же идентификатор
    /// проходит повторно и действие выполняется.
    /// </summary>
    [Test]
    public async Task CommandIdOfFailedActionStaysFreeTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var beforeFailure = await ReadResourceTotal(client);
        var commandId = Guid.NewGuid();

        var failed = await Buy(client, UnknownDomikTypeId, commandId);
        var succeeded = await Upgrade(client, domikId, commandId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failed.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(succeeded.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await ReadResourceTotal(client), Is.LessThan(beforeFailure));
        }
    }

    /// <summary>
    /// Запрос без заголовка команды идёт прежним путём: две отправки списывают ресурсы дважды.
    /// </summary>
    [Test]
    public async Task CommandsWithoutHeaderApplyEveryTimeTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);

        await client.PostAsync($"/Domiki/UpgradeDomik/{domikId}", null);
        var afterFirst = await ReadResourceTotal(client);
        await client.PostAsync($"/Domiki/UpgradeDomik/{domikId}", null);

        Assert.That(await ReadResourceTotal(client), Is.LessThan(afterFirst));
    }

    /// <summary>
    /// Фоновая чистка убирает отметки команд старше срока хранения и оставляет свежие.
    /// </summary>
    [Test]
    public async Task CleanupRemovesExpiredCommandMarksTest()
    {
        const int batchSize = 500;

        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);

        var now = DateTimeHelper.GetNowDate();
        var stale = MarkCommand(playerId, now - IdempotencyFilter.Retention - TimeSpan.FromHours(1));
        var fresh = MarkCommand(playerId, now);

        DeleteExpiredMarks(now - IdempotencyFilter.Retention, batchSize);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(App.Read(context => context.PlayerCommands.Any(x => x.CommandId == stale)), Is.False);
            Assert.That(App.Read(context => context.PlayerCommands.Any(x => x.CommandId == fresh)), Is.True);
        }
    }

    private static Guid MarkCommand(int playerId, DateTime appliedDate)
    {
        var commandId = Guid.NewGuid();
        using var scope = App.Scope();
        scope.Context.PlayerCommands.Add(new()
        {
            PlayerId = playerId,
            CommandId = commandId,
            AppliedDate = appliedDate,
        });

        scope.Commit();
        return commandId;
    }

    private static void DeleteExpiredMarks(DateTime cutoff, int batchSize)
    {
        using var scope = App.Scope();
        PlayerCommandCleanupService.DeleteOlderThan(scope.Context, cutoff, batchSize);
        scope.Commit();
    }

    private static Task<HttpResponseMessage> Upgrade(HttpClient client, int domikId, Guid commandId) =>
        Send(client, $"/Domiki/UpgradeDomik/{domikId}", commandId);

    private static Task<HttpResponseMessage> Buy(HttpClient client, int typeId, Guid commandId) =>
        Send(client, $"/Domiki/BuyDomik/{typeId}", commandId);

    private static Task<HttpResponseMessage> Send(HttpClient client, string url, Guid commandId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation(IdempotencyFilter.HeaderName, commandId.ToString());
        return client.SendAsync(request);
    }

    private static async Task<int> ReadPlayerId(HttpClient client)
    {
        using var state = await ReadState(client);
        return state.RootElement.GetProperty("playerId").GetInt32();
    }

    private static async Task<long> ReadResourceTotal(HttpClient client)
    {
        using var state = await ReadState(client);
        var total = 0L;
        foreach (var resource in state.RootElement.GetProperty("resources").EnumerateArray())
        {
            total += resource.GetProperty("value").GetInt32();
        }

        return total;
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

    private static async Task<JsonDocument> ReadState(HttpClient client)
    {
        var response = await client.GetAsync("/Domiki/GetGameState");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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
