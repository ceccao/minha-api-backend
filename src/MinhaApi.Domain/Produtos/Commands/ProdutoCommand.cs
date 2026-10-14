namespace MinhaApi.Domain.Produtos.Commands;

// Intencao de criar ou editar um produto. Imutavel: so carrega os dados.
public record ProdutoCommand(string Nome, decimal Preco);
