using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Data.Entities;

/// <summary>
/// Один накопительный счётчик игрока за сезон – источник данных для сезонных потоковых рейтингов (заказы, толока, экспедиции).
/// </summary>
/// <remarks>
/// Обнуляется сам собой сменой <see cref="SeasonId"/>, старые строки не удаляются.
/// </remarks>
[PrimaryKey(nameof(SeasonId), nameof(PlayerId), nameof(Metric))]
[Index(nameof(SeasonId), nameof(Metric))]
public class SeasonCounter
{
    /// <summary>
    /// Часть составного ключа – номер сезона (см. <see cref="Village.SeasonManager.GetCurrentSeason"/>).
    /// </summary>
    [Column(Order = 1)]
    public int SeasonId { get; set; }

    /// <summary>
    /// Часть составного ключа – игрок, которому принадлежит счётчик.
    /// </summary>
    [Column(Order = 2)]
    public int PlayerId { get; set; }

    /// <summary>
    /// Часть составного ключа – какая метрика считается.
    /// </summary>
    /// <remarks>
    /// Хранит числовое значение <see cref="Village.Models.SeasonMetric"/> (сама сущность enum не знает, чтобы новая номинация не требовала миграции).
    /// </remarks>
    [Column(Order = 3)]
    public int Metric { get; set; }

    /// <summary>
    /// Накопленное за сезон значение метрики.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Навигационное свойство к игроку.
    /// </summary>
    public Player Player { get; set; } = null!;
}
