using Microsoft.EntityFrameworkCore;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Domain.Produtos.Repositories.Filters;
using MinhaApi.Infra.Config;

namespace MinhaApi.Infra.Produtos.Repositories;

public class ProdutoRepository(MinhaApiDbContext context) : RepositorioBase<Produto>(context), IProdutoRepository
{
    // Somente leitura (alimenta a listagem): AsNoTracking evita rastrear as entidades.
    public IQueryable<Produto> Filtrar(ProdutoListarFilter filtro)
    {
        var query = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Nome))
        {
            query = query.Where(p => p.Nome.Contains(filtro.Nome));
        }

        return query;
    }
}
