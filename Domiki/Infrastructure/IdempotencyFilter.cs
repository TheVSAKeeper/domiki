using System.Security.Claims;
using Domiki.Web.Core;
using Domiki.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Отбивает повторную отправку одной и той же команды игрока: второй раз действие не выполняется, ответ – пустой <c>200</c>.
/// </summary>
/// <remarks>
/// Включается заголовком <see cref="HeaderName"/> с идентификатором команды, который присваивает клиент; запрос без заголовка
/// проходит как прежде, поэтому старый клиент продолжает работать. Отметка <see cref="Data.Entities.PlayerCommand"/> пишется
/// в транзакции запроса до выполнения действия: неудачное действие бросает <see cref="BusinessException"/>, транзакция
/// откатывается вместе с отметкой, и повтор снова дойдёт до менеджера. Гонка двух одновременных отправок ловится уникальным
/// индексом под точкой сохранения – без неё ошибка уникальности перевела бы транзакцию Postgres в состояние aborted и
/// уронила бы коммит <see cref="UnitOfWorkMiddleware"/>. Экшены, возвращающие DTO, фильтр не трогает: отбитый повтор отдал бы
/// пустое тело вместо модели, которую клиент разбирает схемой.
/// </remarks>
public class IdempotencyFilter : IAsyncActionFilter
{
    /// <summary>
    /// Заголовок с идентификатором команды.
    /// </summary>
    public const string HeaderName = "X-Command-Id";

    /// <summary>
    /// Сколько хранятся отметки применённых команд.
    /// </summary>
    /// <remarks>
    /// С запасом больше офлайн-окна: команда, отложенная на несколько часов, должна встретить свою отметку живой.
    /// </remarks>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    /// <summary>
    /// Пропускает действие только один раз на идентификатор команды.
    /// </summary>
    /// <param name="context">Контекст выполняемого действия.</param>
    /// <param name="next">Продолжение конвейера фильтров.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is not GameControllerBase
            || !HttpMethods.IsPost(context.HttpContext.Request.Method)
            || !ReturnsNothing(context.ActionDescriptor)
            || !TryGetCommandId(context.HttpContext.Request, out var commandId))
        {
            await next();
            return;
        }

        var services = context.HttpContext.RequestServices;
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            await next();
            return;
        }

        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var playerId = services.GetRequiredService<DomikManager>().GetPlayerId(userId);

        // TODO: отметка не помнит, каким действием была занята, поэтому тот же идентификатор на другом эндпоинте молча отобьётся успехом; хранить отпечаток маршрута, когда клиент начнёт присваивать идентификатор намерению, а не запросу (шаг 4 docs/offline-play.md)
        if (PlayerCommandMarks.IsApplied(dbContext, playerId, commandId))
        {
            context.Result = new OkResult();
            return;
        }

        if (!PlayerCommandMarks.TryMark(services.GetRequiredService<UnitOfWork>(), dbContext, playerId, commandId))
        {
            context.Result = new OkResult();
            return;
        }

        await next();
    }

    private static bool ReturnsNothing(ActionDescriptor descriptor)
    {
        if (descriptor is not ControllerActionDescriptor controllerAction)
        {
            return false;
        }

        var returnType = controllerAction.MethodInfo.ReturnType;
        return returnType == typeof(void) || returnType == typeof(Task);
    }

    private static bool TryGetCommandId(HttpRequest request, out Guid commandId)
    {
        commandId = default;
        var header = request.Headers[HeaderName].ToString();
        return !string.IsNullOrEmpty(header) && Guid.TryParse(header, out commandId) && commandId != Guid.Empty;
    }
}
