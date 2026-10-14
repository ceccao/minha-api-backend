using MinhaApi.CrossCutting.Enums;

namespace MinhaApi.Domain.Produtos.Repositories.Filters;

public class ProdutoListarFilter
{
    public string? Nome { get; set; }
    public Situacao? Situacao { get; set; }
}
