using System.Security.Claims;
using Domiki.Web.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Возвращает снимок состояния прямо в ответе на действие игрока, если клиент попросил заголовком <see cref="HeaderName"/>.
/// </summary>
/// <remarks>
/// Снимает второй запрос: раньше клиент после каждой мутации отдельно тянул <see cref="GameStateController.GetGameState"/>
/// (по замеру прод-лога 99 % действий сопровождались им в пределах трёх секунд, средний ответ – 12 КБ). Снимок собирает
/// <see cref="GameStateProjector"/>, то есть без одноразовых выдач: витрину «Пока вас не было», гостинец и веху трудяги
/// игрок по-прежнему получает только на <see cref="GameStateController.GetGameState"/>.
/// Подменяется только пустой ответ – экшены, возвращающие свой DTO, остаются как есть; повтор команды, отбитый
/// <see cref="IdempotencyFilter"/>, тоже получает снимок, поэтому клиенту не нужно различать первый ответ и повторный.
/// Собирается снимок до коммита транзакции запроса, в том же контексте, поэтому он уже видит только что сделанное действие.
/// </remarks>
public class GameStateEchoFilter : IAsyncActionFilter
{
    /// <summary>
    /// Заголовок-просьба вернуть состояние вместе с ответом.
    /// </summary>
    public const string HeaderName = "X-With-State";

    /// <summary>
    /// Подменяет пустой ответ действия снимком состояния игрока.
    /// </summary>
    /// <param name="context">Контекст выполняемого действия.</param>
    /// <param name="next">Продолжение конвейера фильтров.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception != null
            || context.Controller is not GameControllerBase
            || !HttpMethods.IsPost(context.HttpContext.Request.Method)
            || context.HttpContext.Request.Headers[HeaderName].ToString() != "1"
            || !IsEmpty(executed.Result))
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return;
        }

        var playerId = services.GetRequiredService<DomikManager>().GetPlayerId(userId);
        executed.Result = new ObjectResult(services.GetRequiredService<GameStateProjector>().Project(playerId));
    }

    private static bool IsEmpty(IActionResult? result) =>
        result is null or EmptyResult || result is OkResult;
}
