using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;

namespace MinhaApi.Domain.Produtos.Services.Interfaces;

public interface IProdutosService
{
    Task<Produto> ValidarAsync(int id, CancellationToken cancellationToken);
    Task<Produto> InserirAsync(ProdutoCommand command, CancellationToken cancellationToken);
    Task<Produto> EditarAsync(int id, ProdutoCommand command, CancellationToken cancellationToken);
    Task<Produto> AtivarAsync(int id, CancellationToken cancellationToken);
    Task<Produto> InativarAsync(int id, CancellationToken cancellationToken);
}
