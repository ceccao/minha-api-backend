using FluentValidation;
using Mapster;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.DataTransfer.Responses;
using MinhaApi.Application.Produtos.Services.Interfaces;
using MinhaApi.CrossCutting.Exceptions;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Domain.Produtos.Commands;
using MinhaApi.Domain.Produtos.Entities;
using MinhaApi.Domain.Produtos.Repositories;
using MinhaApi.Domain.Produtos.Repositories.Filters;
using MinhaApi.Domain.Produtos.Services.Interfaces;

namespace MinhaApi.Application.Produtos.Services;

// Papel da Application: validar o request, controlar a transacao e converter
// request/response. A REGRA DE NEGOCIO fica no Domain (ProdutoDomainService).
public class ProdutoService(
    IProdutoDomainService produtoDomainService,
    IProdutoRepository produtoRepository,
    IUnitOfWork unitOfWork,
    IValidator<ProdutoRequest> produtoValidator,
    IValidator<ListarProdutosRequest> listarValidator) : IProdutoService
{
    public async Task<ProdutoResponse> RecuperarAsync(int id, CancellationToken cancellationToken)
    {
        Produto produto = await produtoDomainService.ValidarAsync(id, cancellationToken);
        return produto.Adapt<ProdutoResponse>();
    }

    // Consulta de leitura: nao ha regra de negocio nem transacao, entao le direto do
    // repositorio (passar pelo Domain Service so repassaria a chamada).
    public async Task<PaginacaoConsulta<ProdutoResponse>> ListarAsync(ListarProdutosRequest request, CancellationToken cancellationToken)
    {
        await ValidarAsync(listarValidator, request, cancellationToken);

        ProdutoListarFilter filtro = request.Adapt<ProdutoListarFilter>();
        IQueryable<Produto> query = produtoRepository.Filtrar(filtro);

        PaginacaoConsulta<Produto> resultado = await produtoRepository.ListarAsync(
            query, request.Qt, request.Pg, request.CpOrd, request.TpOrd, cancellationToken);

        return resultado.Mapear(produto => produto.Adapt<ProdutoResponse>());
    }

    public async Task<ProdutoResponse> InserirAsync(ProdutoRequest request, CancellationToken cancellationToken)
    {
        await ValidarAsync(produtoValidator, request, cancellationToken);
        ProdutoCommand command = request.Adapt<ProdutoCommand>();

        return await ExecutarEmTransacaoAsync(
            () => produtoDomainService.InserirAsync(command, cancellationToken),
            cancellationToken);
    }

    public async Task<ProdutoResponse> EditarAsync(int id, ProdutoRequest request, CancellationToken cancellationToken)
    {
        await ValidarAsync(produtoValidator, request, cancellationToken);
        ProdutoCommand command = request.Adapt<ProdutoCommand>();

        return await ExecutarEmTransacaoAsync(
            () => produtoDomainService.EditarAsync(id, command, cancellationToken),
            cancellationToken);
    }

    public async Task<ProdutoResponse> ExcluirAsync(int id, CancellationToken cancellationToken)
        => await ExecutarEmTransacaoAsync(
            () => produtoDomainService.InativarAsync(id, cancellationToken),
            cancellationToken);

    // Abre a transacao, executa a operacao do Domain e confirma (Commit grava no banco).
    // Qualquer erro desfaz tudo. O response so e montado depois do commit, quando o Id
    // gerado pelo banco ja existe.
    private async Task<ProdutoResponse> ExecutarEmTransacaoAsync(Func<Task<Produto>> operacao, CancellationToken cancellationToken)
    {
        try
        {
            unitOfWork.BeginTransaction();
            Produto produto = await operacao();
            await unitOfWork.CommitAsync(cancellationToken);
            return produto.Adapt<ProdutoResponse>();
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Validacao explicita (FluentValidation.AspNetCore foi descontinuado): o erro vira
    // EntidadeInvalidaException e passa pelo mesmo ExceptionMiddleware (400).
    private static async Task ValidarAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var resultado = await validator.ValidateAsync(request, cancellationToken);

        if (!resultado.IsValid)
        {
            throw new EntidadeInvalidaException(resultado.Errors.Select(e => e.ErrorMessage));
        }
    }
}
