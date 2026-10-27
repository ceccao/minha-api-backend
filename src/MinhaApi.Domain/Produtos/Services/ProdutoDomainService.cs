using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Domain.Produtos.Services.Interfaces;

namespace MinhaApi.Domain.Produtos.Services;

public class ProdutoDomainService(IProdutoRepository produtoRepository) : IProdutoDomainService
{
    public async Task<Produto> ValidarAsync(int id, CancellationToken cancellationToken)
    {
        Produto produto = await produtoRepository.RecuperarAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException<Produto>(id);

        return produto;
    }

    public async Task<Produto> InserirAsync(ProdutoCommand command, CancellationToken cancellationToken)
    {
        Produto produto = new(
            command.Nome,
            command.Preco
            );

        await produtoRepository.InserirAsync(produto, cancellationToken);

        return produto;
    }

    public async Task<Produto> EditarAsync(int id, ProdutoCommand command, CancellationToken cancellationToken)
    {
        Produto produto = await ValidarAsync(id, cancellationToken);

        // Cliente enviou a versao que leu: se mudou, outra pessoa editou antes.
        if (command.Version.HasValue && command.Version.Value != produto.Version)
        {
            throw new ConflitoException("O produto foi modificado por outra operação. Recarregue e tente novamente.");
        }

        produto.SetNome(command.Nome);
        produto.SetPreco(command.Preco);

        await produtoRepository.EditarAsync(produto, cancellationToken);

        return produto;
    }

    public async Task<Produto> AtivarAsync(int id, CancellationToken cancellationToken)
    {
        Produto produto = await ValidarAsync(id, cancellationToken);

        produto.Ativar();
        await produtoRepository.EditarAsync(produto, cancellationToken);

        return produto;
    }

    public async Task<Produto> InativarAsync(int id, CancellationToken cancellationToken)
    {
        Produto produto = await ValidarAsync(id, cancellationToken);

        produto.Inativar();
        await produtoRepository.EditarAsync(produto, cancellationToken);

        return produto;
    }
}
