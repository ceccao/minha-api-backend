namespace MinhaApi.Domain.Produtos.Commands;

// Intencao de criar ou editar um produto. Imutavel: so carrega os dados.
// Version e opcional: se o cliente a enviar na edicao, e comparada com a versao atual
// (protege contra "lost update": duas pessoas editando o mesmo produto).
public record ProdutoCommand(string Nome, decimal Preco, int? Version = null);
