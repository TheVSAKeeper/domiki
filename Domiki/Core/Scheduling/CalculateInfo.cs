namespace Domiki.Web.Core.Scheduling;

public class CalculateInfo
{
    public int PlayerId { get; set; }
    public int ObjectId { get; set; }

    /// <summary>
    /// дата когда событие должно выполнится
    /// </summary>
    public DateTime Date { get; set; }

    public CalculateTypes Type { get; set; }

    public string? PushTitle { get; set; }
    public string? PushBody { get; set; }

    /// <summary>
    /// Категория Web Push-уведомления, которая попадёт в tag service worker.
    /// </summary>
    /// <remarks>
    /// Нужна, когда текст уведомления создаётся одним событием, а планировщик обрабатывает другое,
    /// например при старте происшествия на завершившемся походе.
    /// </remarks>
    public string? PushTag { get; set; }
}
