using Mapster;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.DataTransfer.Responses;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories.Filters;

namespace MinhaApi.Application.Produtos.Profiles;

// O Mapster esta configurado para EXIGIR mapeamento explicito (ver IoC): todo par
// origem/destino usado no codigo precisa estar registrado aqui. Esquecer um
// mapeamento falha na hora, em vez de devolver campos vazios em silencio.
public class ProdutoProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Produto, ProdutoResponse>();
        config.NewConfig<ProdutoRequest, ProdutoCommand>();
        config.NewConfig<ListarProdutosRequest, ProdutoListarFilter>();
    }
}
