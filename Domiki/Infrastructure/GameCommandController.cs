using Domiki.Web.Core;
using Domiki.Web.Infrastructure.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Приём пачки намерений игрока.
/// </summary>
public class GameCommandController : GameControllerBase
{
    private readonly GameCommandBatch _batch;
    private readonly GameStateProjector _projector;

    public GameCommandController(DomikManager domikManager, GameCommandBatch batch, GameStateProjector projector)
        : base(domikManager)
    {
        _batch = batch;
        _projector = projector;
    }

    /// <summary>
    /// Применяет пачку намерений и возвращает итог каждого вместе с состоянием после них.
    /// </summary>
    /// <param name="batch">Команды в порядке отправки.</param>
    /// <returns>Итоги команд и снимок состояния.</returns>
    /// <remarks>
    /// Заменяет цепочку из отдельных запросов: один лок строки игрока, одна транзакция, один снимок в ответе.
    /// Отвергнутая команда не роняет остальные – её итог приходит отдельной записью с текстом для игрока.
    /// Пачка от чужого игрока отбивается целиком и до применения: на общем устройстве очередь намерений переживает
    /// смену учётной записи, и без этой сверки они уехали бы в чужую деревню.
    /// </remarks>
    /// <exception cref="BusinessException">Пачка называет игрока, отличного от текущей сессии.</exception>
    [HttpPost]
    [Route("/Domiki/ApplyCommands")]
    public GameCommandBatchResultDto ApplyCommands([FromBody] GameCommandBatchDto batch)
    {
        var playerId = GetPlayerId();
        if (batch.PlayerId is int declaredPlayerId && declaredPlayerId != playerId)
        {
            throw new BusinessException("Эти дела начаты из другой деревни – здесь их не выполнить");
        }

        var results = _batch.Apply(playerId, batch.Commands);

        return new() { Results = results, State = _projector.Project(playerId) };
    }
}
