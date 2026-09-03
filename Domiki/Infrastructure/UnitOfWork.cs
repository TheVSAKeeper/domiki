using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domiki.Web.Infrastructure;

public class UnitOfWork : IDisposable
{
    private readonly List<Action> afterEventActions = [];

    private bool isRollbacked;
    private bool isCommitted;

    public UnitOfWork(ApplicationDbContext context)
    {
        Transaction = context.Database.BeginTransaction();
        Context = context;
    }

    public IDbContextTransaction Transaction { get; }
    public ApplicationDbContext Context { get; }

    /// <summary>
    /// Откладывает действие до успешного коммита транзакции: планировщик получает событие только тогда, когда запись
    /// действительно сохранена.
    /// </summary>
    /// <param name="action">Что выполнить после коммита.</param>
    /// <remarks>
    /// Действия копятся списком и выполняются в порядке добавления. Списком, а не одним полем: раньше каждый вызывающий
    /// сам сцеплял своё действие с предыдущим, и достаточно было одного забытого сцепления, чтобы событие соседней
    /// операции пропало. В одном запросе таких операций бывает несколько.
    /// </remarks>
    public void AddAfterEventAction(Action action) => afterEventActions.Add(action);

    /// <summary>
    /// Сколько отложенных действий уже накоплено.
    /// </summary>
    /// <remarks>
    /// Отметка для отката: пачка команд запоминает её перед командой и обрезает по ней список, если команда отвергнута.
    /// </remarks>
    public int AfterEventActionCount => afterEventActions.Count;

    /// <summary>
    /// Отбрасывает отложенные действия, добавленные после отметки.
    /// </summary>
    /// <param name="count">Отметка, снятая <see cref="AfterEventActionCount"/> до выполнения команды.</param>
    public void DropAfterEventActionsAfter(int count) => afterEventActions.RemoveRange(count, afterEventActions.Count - count);

    public void Commit()
    {
        if (isCommitted || isRollbacked)
        {
            throw new("commit or rollback has been called.");
        }

        Context.SaveChanges();
        Transaction.Commit();
        foreach (var afterEventAction in afterEventActions)
        {
            afterEventAction();
        }

        isCommitted = true;
    }

    public void Rollback()
    {
        if (isCommitted || isRollbacked)
        {
            throw new("commit or rollback has been called.");
        }

        Transaction.Rollback();
        isRollbacked = true;
    }

    public void Dispose()
    {
        if (!isCommitted && !isRollbacked)
        {
            Rollback();
        }

        Transaction.Dispose();
    }
}
