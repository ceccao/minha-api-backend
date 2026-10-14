using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.DataTransfer.Responses;
using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Application.Produtos.Services.Interfaces;

public interface IProdutoService
{
    Task<ProdutoResponse> RecuperarAsync(int id, CancellationToken cancellationToken);
    Task<PaginacaoConsulta<ProdutoResponse>> ListarAsync(ListarProdutosRequest request, CancellationToken cancellationToken);
    Task<ProdutoResponse> InserirAsync(ProdutoRequest request, CancellationToken cancellationToken);
    Task<ProdutoResponse> EditarAsync(int id, ProdutoRequest request, CancellationToken cancellationToken);
    Task<ProdutoResponse> ExcluirAsync(int id, CancellationToken cancellationToken);
}
