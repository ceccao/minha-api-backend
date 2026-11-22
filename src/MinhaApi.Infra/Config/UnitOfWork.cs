using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MinhaApi.CrossCutting.Exceptions;
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
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            if (_transacao is not null)
            {
                await _transacao.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // O estado rastreado ficou desatualizado: limpa e traduz para a exception do CrossCutting.
            context.ChangeTracker.Clear();
            throw new ConflitoException("O registro");
        }
        finally
        {
            await EncerrarTransacaoAsync();
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is not null)
        {
            await _transacao.RollbackAsync(cancellationToken);
        }

        await EncerrarTransacaoAsync();
    }

    private async Task EncerrarTransacaoAsync()
    {
        if (_transacao is not null)
        {
            await _transacao.DisposeAsync();
            _transacao = null;
        }
    }
}
