using MinhaApi.Domain.Abstractions;

namespace MinhaApi.Application.Produtos.DataTransfer.Requests;

public class ListarProdutosRequest : PaginacaoFiltro
{
    public string? Nome { get; set; }
    public decimal? PrecoMinimo { get; set; }
    public decimal? PrecoMaximo { get; set; }
    public bool? Ativo { get; set; }

    public ListarProdutosRequest() : base(cpOrd: "Id", tpOrd: TipoOrdenacao.Ascendente)
    {
    }
}
