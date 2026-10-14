using Microsoft.EntityFrameworkCore.Storage;
using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Infra.Config;

public class UnitOfWork(MinhaApiDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transacao;

    public void BeginTransaction()
    {
        _transacao = context.Database.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is null)
        {
            return;
        }

        await _transacao.CommitAsync(cancellationToken);
        await _transacao.DisposeAsync();
        _transacao = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is null)
        {
            return;
        }

        await _transacao.RollbackAsync(cancellationToken);
        await _transacao.DisposeAsync();
        _transacao = null;
    }
}
