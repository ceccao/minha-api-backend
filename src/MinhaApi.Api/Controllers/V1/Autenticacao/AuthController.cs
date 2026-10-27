using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MinhaApi.Api.Configuration;
using MinhaApi.Application.Autenticacao.DataTransfer;
using MinhaApi.Application.Autenticacao.Services.Interfaces;

namespace MinhaApi.Api.Controllers.V1.Autenticacao;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController(IAutenticacaoService autenticacaoService) : ControllerBase
{
    /// <summary>
    /// Autentica com e-mail e senha e devolve um token JWT (enviar em Authorization: Bearer).
    /// </summary>
    /// <param name="request">E-mail e senha.</param>
    /// <param name="cancellationToken"></param>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitConfiguration.PoliticaLogin)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await autenticacaoService.LoginAsync(request, cancellationToken));
}
