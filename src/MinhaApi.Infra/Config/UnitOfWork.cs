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
            // Outra operacao alterou ou apagou a linha. Limpa o contexto (o estado
            // rastreado ficou desatualizado) e traduz para a exception do CrossCutting,
            // que nao conhece o ORM.
            context.ChangeTracker.Clear();
            throw new ConflitoException("O recurso foi modificado por outra operação. Recarregue e tente novamente.");
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
