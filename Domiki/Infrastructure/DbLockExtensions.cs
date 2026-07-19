using Microsoft.EntityFrameworkCore;

namespace Domiki.Web.Infrastructure;

/// <summary>
/// Расширения <see cref="DbContext"/> для пессимистичных блокировок строк.
/// </summary>
public static class DbLockExtensions
{
    /// <summary>
    /// Берёт блокировку строки сущности <typeparamref name="TEntity"/> по первичному ключу (<c>SELECT ... FOR UPDATE</c>) до конца текущей транзакции.
    /// </summary>
    /// <remarks>
    /// Имена таблицы и ключевого столбца читаются из EF-модели, а не хардкодятся, поэтому лок переживает переименование схемы (в частности, snake_case-конвенцию). Значение ключа уходит SQL-параметром, идентификаторы приходят из доверенной модели, поэтому инъекция исключена. Осмысленно только внутри уже открытой транзакции.
    /// </remarks>
    /// <typeparam name="TEntity">Сущность, чья строка блокируется; предполагается одностолбцовый первичный ключ.</typeparam>
    /// <param name="context">Контекст, в транзакции которого берётся блокировка.</param>
    /// <param name="keyValue">Значение первичного ключа блокируемой строки.</param>
    public static void LockRowForUpdate<TEntity>(this DbContext context, object keyValue)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Тип {typeof(TEntity).Name} не найден в модели EF.");
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException($"У сущности {typeof(TEntity).Name} нет таблицы.");
        var keyColumn = entityType.FindPrimaryKey()!.Properties[0].GetColumnName();
        context.Database.ExecuteSqlRaw($"SELECT 1 FROM \"{table}\" WHERE \"{keyColumn}\" = {{0}} FOR UPDATE", keyValue);
    }
}
