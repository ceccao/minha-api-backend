using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace MinhaApi.CrossCutting.Middlewares;

public class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    private const string CspPadrao = "default-src 'self'";

    // O Swagger UI (so habilitado em Development) usa estilo inline e imagens data:,
    // que a CSP restrita bloqueia. Fora de Development a CSP continua restrita.
    private const string CspDevelopment = "default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.ContentSecurityPolicy = environment.IsDevelopment() ? CspDevelopment : CspPadrao;

        await next(context);
    }
}
