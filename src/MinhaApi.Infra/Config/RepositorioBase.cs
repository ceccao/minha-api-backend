using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Infra.Config;

// Repositorio generico. As escritas so registram a mudanca no DbContext; a gravacao
// acontece uma unica vez no UnitOfWork.CommitAsync.
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
        // Se a entidade ja veio rastreada de um RecuperarAsync o Update e
        // desnecessario, mas inofensivo.
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

    // FindAsync consulta o cache do contexto antes de ir ao banco.
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

    // Monta a clausula no formato do System.Linq.Dynamic.Core (ex: "Preco descending").
    // Diferente de SQL cru: o Dynamic.Core valida o campo como propriedade real de
    // TEntidade antes de montar a expressao. Quais campos a API aceita ordenar e
    // decidido pelo validator do request (Application).
    private static string ClausulaOrdenacao(string campo, TipoOrdenacao tipo)
        => $"{campo} {(tipo == TipoOrdenacao.Descendente ? "descending" : "ascending")}";
}
