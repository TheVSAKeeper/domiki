using System.Net;
using System.Text.Json;
using Domiki.Web.Infrastructure;

namespace Domiki.Web.Tests;

public sealed class GameStateEchoTests
{
    private const int GrantedResourceValue = 100000;

    /// <summary>
    /// Действие с просьбой о состоянии отвечает снимком игрока, в котором уже видно результат самого действия.
    /// </summary>
    [Test]
    public async Task ActionWithStateHeaderReturnsSnapshotTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var response = await Upgrade(client, domikId, withState: true);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var state = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var upgraded = state.RootElement.GetProperty("domiks").EnumerateArray()
            .First(domik => domik.GetProperty("id").GetInt32() == domikId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.RootElement.GetProperty("playerId").GetInt32(), Is.GreaterThan(0));
            Assert.That(upgraded.GetProperty("upgradeSeconds").ValueKind, Is.Not.EqualTo(JsonValueKind.Null));
        }
    }

    /// <summary>
    /// Снимок в ответе на действие не съедает витрину «Пока вас не было»: она остаётся пустой и достаётся снимку состояния.
    /// </summary>
    [Test]
    public async Task ActionSnapshotLeavesRecapEmptyTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var response = await Upgrade(client, domikId, withState: true);

        using var state = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var recap = state.RootElement.GetProperty("recap");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recap.GetProperty("events").GetArrayLength(), Is.Zero);
            Assert.That(recap.GetProperty("awaySeconds").GetInt32(), Is.Zero);
        }
    }

    /// <summary>
    /// Без заголовка ответ на действие остаётся пустым – прежний контракт для старого клиента цел.
    /// </summary>
    [Test]
    public async Task ActionWithoutHeaderReturnsEmptyBodyTest()
    {
        var client = App.Client();
        await client.PostAsync("/authentication/demo", null);
        var playerId = await ReadPlayerId(client);
        GrantResources(playerId);

        var domikId = CreateUpgradableDomik(playerId);
        var response = await Upgrade(client, domikId, withState: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.Empty);
        }
    }

    private static Task<HttpResponseMessage> Upgrade(HttpClient client, int domikId, bool withState)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/Domiki/UpgradeDomik/{domikId}");
        if (withState)
        {
            request.Headers.TryAddWithoutValidation(GameStateEchoFilter.HeaderName, "1");
        }

        return client.SendAsync(request);
    }

    private static async Task<int> ReadPlayerId(HttpClient client)
    {
        using var state = await ReadState(client);
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
