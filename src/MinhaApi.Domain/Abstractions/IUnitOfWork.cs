namespace MinhaApi.Domain.Abstractions;

public interface IUnitOfWork
{
    void BeginTransaction();
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
