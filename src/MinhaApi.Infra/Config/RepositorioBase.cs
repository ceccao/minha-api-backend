using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace MinhaApi.Infra.Config;

public class RepositorioBase<TEntidade>(MinhaApiDbContext context) : IRepositorioBase<TEntidade>
    where TEntidade : class
{
    protected MinhaApiDbContext Context { get; } = context;

    protected DbSet<TEntidade> Set => Context.Set<TEntidade>();

    public virtual async Task InserirAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entidade, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task InserirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default)
    {
        await Set.AddRangeAsync(entidades, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task EditarAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        try
        {
            // Se a entidade ja veio rastreada de um RecuperarAsync o Update e
            // desnecessario, mas inofensivo.
            Context.Update(entidade);
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // O estado rastreado ficou desatualizado: limpa o contexto para que uma
            // nova tentativa recarregue do banco, e traduz para a exception do
            // CrossCutting (que nao conhece o ORM - README §7.3).
            Context.ChangeTracker.Clear();
            throw new ConflitoException("O recurso foi modificado por outra operação. Recarregue e tente novamente.");
        }
    }

    public virtual async Task ExcluirAsync(TEntidade entidade, CancellationToken cancellationToken = default)
    {
        Set.Remove(entidade);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task ExcluirAsync(IEnumerable<TEntidade> entidades, CancellationToken cancellationToken = default)
    {
        Set.RemoveRange(entidades);
        await Context.SaveChangesAsync(cancellationToken);
    }

    // FindAsync consulta o cache do contexto antes de ir ao banco.
    public virtual async Task<TEntidade> RecuperarAsync(int id, CancellationToken cancellationToken = default)
        => (await Set.FindAsync([id], cancellationToken))!; // contrato do Domain (nullable desabilitado) devolve null se nao achar

    public virtual async Task<TEntidade> RecuperarAsync(Expression<Func<TEntidade, bool>> expressao, CancellationToken cancellationToken = default)
        => (await Set.FirstOrDefaultAsync(expressao, cancellationToken))!;

    public virtual Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        PaginacaoFiltro paginacao,
        Expression<Func<TEntidade, bool>>? filtro = null,
        CancellationToken cancellationToken = default)
        => ListarInternoAsync(
            paginacao.Qt, paginacao.Pg, filtro,
            query => string.IsNullOrWhiteSpace(paginacao.CpOrd)
                ? query
                : query.OrderBy(ClausulaOrdenacao(paginacao.CpOrd, paginacao.TpOrd)),
            cancellationToken);

    // Overload que recebe o IQueryable JA FILTRADO (via IProdutoRepository.Filtrar,
    // por exemplo) - so aplica paginacao e ordenacao por um unico campo em cima dele.
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

    public virtual Task<PaginacaoConsulta<TEntidade>> ListarAsync(
        int qt,
        int pg,
        (string Campo, TipoOrdenacao Tipo)[] ordenacao,
        Expression<Func<TEntidade, bool>>? filtro = null,
        CancellationToken cancellationToken = default)
        => ListarInternoAsync(
            qt, pg, filtro,
            query => ordenacao.Length == 0 ? query : query.OrderBy(ClausulaOrdenacao(ordenacao)),
            cancellationToken);

    private async Task<PaginacaoConsulta<TEntidade>> ListarInternoAsync(
        int qt,
        int pg,
        Expression<Func<TEntidade, bool>>? filtro,
        Func<IQueryable<TEntidade>, IQueryable<TEntidade>> aplicarOrdenacao,
        CancellationToken cancellationToken)
    {
        var query = Set.AsNoTracking();

        if (filtro is not null)
        {
            query = query.Where(filtro);
        }

        var totalItens = await query.CountAsync(cancellationToken);

        query = aplicarOrdenacao(query);

        var itens = await query
            .Skip((pg - 1) * qt)
            .Take(qt)
            .ToListAsync(cancellationToken);

        return new PaginacaoConsulta<TEntidade>(itens, totalItens, pg, qt);
    }

    // Monta a clausula no formato exigido pelo System.Linq.Dynamic.Core (ex: "Preco
    // descending"). Diferente de SQL cru: o Dynamic.Core valida "Preco" como uma
    // propriedade real de TEntidade via reflection antes de montar a expressao -
    // um nome invalido lanca ParseException, nunca executa nada no banco.
    private static string ClausulaOrdenacao(string campo, TipoOrdenacao tipo)
        => $"{campo} {(tipo == TipoOrdenacao.Descendente ? "descending" : "ascending")}";

    private static string ClausulaOrdenacao((string Campo, TipoOrdenacao Tipo)[] ordenacao)
        => string.Join(", ", ordenacao.Select(o => ClausulaOrdenacao(o.Campo, o.Tipo)));
}
