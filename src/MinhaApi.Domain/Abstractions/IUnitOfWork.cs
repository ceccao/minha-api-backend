namespace MinhaApi.Domain.Abstractions;

public interface IUnitOfWork
{
    void BeginTransaction();

    // Grava no banco todas as mudancas pendentes e confirma a transacao.
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
