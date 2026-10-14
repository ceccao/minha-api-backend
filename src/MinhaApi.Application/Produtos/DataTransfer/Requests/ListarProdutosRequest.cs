using MinhaApi.CrossCutting.Enums;
using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Application.Produtos.DataTransfer.Requests;

public class ListarProdutosRequest : PaginacaoFiltro
{
    public string? Nome { get; set; }
    public Situacao? Situacao { get; set; }

    public ListarProdutosRequest() : base(cpOrd: "Id", tpOrd: TipoOrdenacao.Ascendente)
    {
    }
}
