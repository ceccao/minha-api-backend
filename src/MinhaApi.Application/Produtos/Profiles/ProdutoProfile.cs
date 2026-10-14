using Mapster;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.DataTransfer.Responses;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;

namespace MinhaApi.Application.Produtos.Profiles;

public class ProdutoProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Produto, ProdutoResponse>();
        config.NewConfig<ProdutoRequest, ProdutoCommand>();
    }
}
