using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinhaApi.Domain.Abstractions;
using MinhaApi.Application.Produtos.DataTransfer.Requests;
using MinhaApi.Application.Produtos.DataTransfer.Responses;
using MinhaApi.Application.Produtos.Services.Interfaces;

namespace MinhaApi.Api.Controllers.V1.Produtos;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProdutosController(IProdutoService produtoService) : ControllerBase
{
    /// <summary>
    /// Lista produtos com filtros opcionais e paginação.
    /// </summary>
    /// <param name="request">Parâmetros de paginação, ordenação e filtro.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [ProducesResponseType<PaginacaoConsulta<ProdutoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAsync([FromQuery] ListarProdutosRequest request, CancellationToken cancellationToken)
        => Ok(await produtoService.ListarAsync(request, cancellationToken));

    /// <summary>
    /// Recupera um produto pelo seu ID.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecuperarAsync(int id, CancellationToken cancellationToken)
        => Ok(await produtoService.RecuperarAsync(id, cancellationToken));

    /// <summary>
    /// Cria um novo produto.
    /// </summary>
    /// <param name="request">Dados do produto a ser criado.</param>
    /// <param name="cancellationToken"></param>
    [HttpPost]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InserirAsync(ProdutoRequest request, CancellationToken cancellationToken)
    {
        var produto = await produtoService.InserirAsync(request, cancellationToken);
        return CreatedAtAction(nameof(RecuperarAsync), new { id = produto.Id }, produto);
    }

    /// <summary>
    /// Atualiza um produto existente.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <param name="request">Novos dados do produto.</param>
    /// <param name="cancellationToken"></param>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarAsync(int id, ProdutoRequest request, CancellationToken cancellationToken)
        => Ok(await produtoService.EditarAsync(id, request, cancellationToken));

    /// <summary>
    /// Exclui um produto existente.
    /// </summary>
    /// <param name="id">Identificador do produto.</param>
    /// <param name="cancellationToken"></param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExcluirAsync(int id, CancellationToken cancellationToken)
    {
        await produtoService.ExcluirAsync(id, cancellationToken);
        return NoContent();
    }
}
