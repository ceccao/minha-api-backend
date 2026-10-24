namespace MinhaApi.Application.Produtos.DataTransfer.Requests;

public class ProdutoRequest
{
    public required string Nome { get; set; }
    public decimal Preco { get; set; }

    /// <summary>
    /// Opcional na edicao: a Version recebida na leitura. Se outra pessoa alterou o
    /// produto no meio tempo, a API responde 409 em vez de sobrescrever.
    /// </summary>
    public int? Version { get; set; }
}
