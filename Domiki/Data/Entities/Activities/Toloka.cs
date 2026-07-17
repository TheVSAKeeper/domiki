using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Инстанция общей толоки – онлайн-активности деревни, куда все игроки сдают ресурс в общий счётчик.
/// </summary>
/// <remarks>
/// По достижении цели выдаёт бафф участникам и запускает следующую инстанцию.
/// </remarks>
[Table("Tolokas")]
public class Toloka
{
    /// <summary>
    /// Идентификатор инстанции толоки.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Тип толоки, выбранный при старте этой инстанции – ссылка на <see cref="TolokaType"/>.
    /// </summary>
    public int TolokaTypeId { get; set; }

    /// <summary>
    /// Суммарно собрано ресурса всеми игроками с начала инстанции.
    /// </summary>
    public int Collected { get; set; }

    /// <summary>
    /// Целевое количество ресурса, при достижении которого толока завершается.
    /// </summary>
    /// <remarks>
    /// Масштабируется числом участников предыдущей инстанции (см. <see cref="Activities.TolokaManager.Contribute"/>).
    /// </remarks>
    public int Goal { get; set; }

    /// <summary>
    /// Момент старта этой инстанции толоки.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Момент достижения цели.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> – толока ещё активна и принимает вклады (в любой момент существует ровно одна такая строка).
    /// </remarks>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Навигационное свойство к типу толоки.
    /// </summary>
    public TolokaType TolokaType { get; set; } = null!;
}
