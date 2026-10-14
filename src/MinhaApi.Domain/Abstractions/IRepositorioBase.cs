using System.Linq.Expressions;

namespace MinhaApi.Domain.Abstractions;

public interface IRepositorioBase<TEntidade> where TEntidade : class
{
    Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default);
    Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default);
    Task<TEntidade> RecuperarAsync(int id, CancellationToken cancellationToken = default);
    Task<TEntidade> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default);

    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        PaginacaoFiltro paginacao,
        Expression<Func<TEntidade, bool>> filtro = null,
        CancellationToken cancellationToken = default);
    Task<PaginacaoConsulta<TEntidade>> ListarAsync(IQueryable<TEntidade> query, int qt, int pg, string cpOrd, TipoOrdenacao tpOrd, CancellationToken cancellationToken);

    Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        int qt,
        int pg,
        (string Campo, TipoOrdenacao Tipo)[] ordenacao,
        Expression<Func<TEntidade, bool>> filtro = null,
        CancellationToken cancellationToken = default);
}
