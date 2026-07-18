using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Голос одного игрока за тип следующей толоки в текущей инстанции.
/// </summary>
/// <remarks>
/// Одна строка на пару (толока, игрок) – голос перевыставляется (UPDATE) при смене выбора.
/// Подсчитывается в ветке завершения <see cref="Activities.TolokaManager.Contribute"/> перед посевом следующей толоки:
/// побеждает тип с наибольшим числом голосов, ничья решается случайно, ноль голосов – взвешенный random <see cref="TolokaType.RotationWeight"/>.
/// </remarks>
[Table("TolokaVotes")]
public class TolokaVote
{
    /// <summary>
    /// Часть составного ключа – инстанция толоки, в которой отдан голос.
    /// </summary>
    [Key]
    [Column(Order = 1)]
    public int TolokaId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок, отдавший голос.
    /// </summary>
    [Key]
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Тип толоки, за который проголосовал игрок, – ссылка на справочник <see cref="TolokaType.Id"/>.
    /// </summary>
    public int CandidateTolokaTypeId { get; set; }

    /// <summary>
    /// Навигационное свойство к инстанции толоки.
    /// </summary>
    public Toloka Toloka { get; set; } = null!;
}
