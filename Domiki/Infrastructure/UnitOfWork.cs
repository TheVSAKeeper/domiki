using Domiki.Web.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domiki.Web.Infrastructure;

public class UnitOfWork : IDisposable
{
    private bool isRollbacked;
    private bool isCommitted;

    public UnitOfWork(ApplicationDbContext context)
    {
        Transaction = context.Database.BeginTransaction();
        Context = context;
    }

    public IDbContextTransaction Transaction { get; }
    public ApplicationDbContext Context { get; }

    public Action AfterEventAction { get; set; }

    public void Commit()
    {
        if (isCommitted || isRollbacked)
        {
            throw new("commit or rollback has been called.");
        }

        Context.SaveChanges();
        Transaction.Commit();
        AfterEventAction?.Invoke();
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
