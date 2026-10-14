using MinhaApi.CrossCutting.Enums;

namespace MinhaApi.Application.Produtos.DataTransfer.Responses;

public class ProdutoResponse
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public decimal Preco { get; set; }
    public Situacao Situacao { get; set; }
}
