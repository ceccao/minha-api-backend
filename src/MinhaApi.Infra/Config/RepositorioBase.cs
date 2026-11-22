using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Infra.Config;

public class RepositorioBase<TEntidade>(MinhaApiDbContext context) : IRepositorioBase<TEntidade>
    where TEntidade : class
{
    protected MinhaApiDbContext Context { get; } = context;

    protected DbSet<TEntidade> Set => Context.Set<TEntidade>();

    public virtual async Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entidade, cancellationToken);

    public virtual async Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default)
        => await Set.AddRangeAsync(entidades, cancellationToken);

    public virtual Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        Context.Update(entidade);
        return Task.CompletedTask;
    }

    public virtual Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        Set.Remove(entidade);
        return Task.CompletedTask;
    }

    public virtual Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default)
    {
        Set.RemoveRange(entidades);
        return Task.CompletedTask;
    }

    public virtual async Task<TEntidade?> RecuperarAsync(int id, CancellationToken cancellationToken = default)
        => await Set.FindAsync([id], cancellationToken);

    public virtual async Task<TEntidade?> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default)
        => await Set.FirstOrDefaultAsync(expressao, cancellationToken);

    public virtual async Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        IQueryable<TEntidade> query, int qt, int pg, string cpOrd, TipoOrdenacao tpOrd, CancellationToken cancellationToken)
    {
        var totalItens = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(cpOrd))
        {
            query = query.OrderBy(ClausulaOrdenacao(cpOrd, tpOrd));
        }

        var itens = await query
            .Skip((pg - 1) * qt)
            .Take(qt)
            .ToListAsync(cancellationToken);

        return new PaginacaoConsulta<TEntidade>(itens, totalItens, pg, qt);
    }

    // O Dynamic.Core valida o campo como propriedade real da entidade antes de montar a expressão.
    private static string ClausulaOrdenacao(string campo, TipoOrdenacao tipo)
        => $"{campo} {(tipo == TipoOrdenacao.Descendente ? "descending" : "ascending")}";
}
