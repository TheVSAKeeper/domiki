using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Отметка о применённой команде игрока – ключ идемпотентности повторной отправки.
/// </summary>
/// <remarks>
/// Клиент присваивает действию идентификатор и повторяет отправку с тем же значением, если ответ не дошёл.
/// <see cref="Infrastructure.IdempotencyFilter"/> вставляет отметку в транзакции запроса до выполнения действия,
/// поэтому повтор упирается в уникальный индекс <c>(PlayerId, CommandId)</c> и завершается без второго списания.
/// Отметки живут <see cref="Infrastructure.IdempotencyFilter.Retention"/>, дальше их удаляет
/// <see cref="Infrastructure.PlayerCommandCleanupService"/>. Индексы: <c>(PlayerId, CommandId)</c> – уникальность,
/// <c>(AppliedDate)</c> – чистка по сроку.
/// </remarks>
[Index(nameof(PlayerId), nameof(CommandId), IsUnique = true)]
[Index(nameof(AppliedDate))]
public class PlayerCommand
{
    /// <summary>
    /// Идентификатор отметки.
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Игрок, чья команда применена.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Идентификатор команды, присвоенный клиентом.
    /// </summary>
    public Guid CommandId { get; set; }

    /// <summary>
    /// Момент применения команды.
    /// </summary>
    /// <value>Момент в UTC.</value>
    public DateTime AppliedDate { get; set; }
}
