using System.Linq.Expressions;

namespace MinhaApi.Domain.Abstractions;

// As operacoes de escrita so marcam a mudanca no contexto; quem grava no banco e o
// IUnitOfWork.CommitAsync (uma unica gravacao, dentro da transacao da Application).
public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default);

    // Recebe a query JA filtrada (ex: IProdutoRepository.Filtrar) e so aplica
    // ordenacao e paginacao em cima dela.
    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        IQueryable<TEntidade> query, int qt, int pg, string cpOrd, TipoOrdenacao tpOrd, CancellationToken cancellationToken);
}
