using System.Linq.Expressions;

namespace MinhaApi.Domain.Abstractions;

// As escritas só marcam a mudança no contexto; quem grava é o IUnitOfWork.CommitAsync.
public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default);

    // Recebe a query já filtrada e só aplica ordenação e paginação.
    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        IQueryable<TEntidade> query, int qt, int pg, string cpOrd, TipoOrdenacao tpOrd, CancellationToken cancellationToken);
}
