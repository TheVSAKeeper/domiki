using Domiki.Web.Data.Entities;
using Domiki.Web.Infrastructure;
using Domiki.Web.Infrastructure.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Domiki.Web.Tests;

[NonParallelizable]
public sealed class WikiStateTests
{
    /// <summary>
    /// Справочник отдаёт справочные данные, не трогая витрину «Пока вас не было»: непоказанное событие после
    /// GetWikiState по-прежнему приезжает следующим снимком состояния игры.
    /// </summary>
    [Test]
    public void WikiStateKeepsRecapUndeliveredTest()
    {
        var player = TestPlayer.Create();
        player.RecordManufactureFinished(DomikIds.Forge);

        var wiki = player.WikiState();
        Assert.That(wiki.DomikTypes, Is.Not.Empty);

        var recap = player.GameState().Recap;
        Assert.That(recap.Events.Select(x => x.Type), Does.Contain(nameof(PlayerEventType.ManufactureFinished)));
    }
}

file static class WikiStateTestsActs
{
    public static WikiStateDto WikiState(this TestPlayer player)
    {
        return Call(player, controller => controller.GetWikiState());
    }

    public static GameStateDto GameState(this TestPlayer player)
    {
        return Call(player, controller => controller.GetGameState());
    }

    public static TestPlayer RecordManufactureFinished(this TestPlayer player, int domikTypeId)
    {
        App.Act<PlayerEventManager>(m => m.RecordManufactureFinished(player.Id, domikTypeId, new() { { ResourceIds.Brick, 1 } }));
        return player;
    }

    private static TResult Call<TResult>(TestPlayer player, Func<GameStateController, TResult> call)
    {
        var aspNetUserId = App.Read(context => context.Players.Single(x => x.Id == player.Id).AspNetUserId);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, aspNetUserId)], "Test");

        using var scope = App.Scope();
        var controller = ActivatorUtilities.CreateInstance<GameStateController>(scope.Services);
        controller.ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new(identity) } };

        var result = call(controller);
        scope.Commit();
        return result;
    }
}
