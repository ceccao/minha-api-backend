using MinhaApi.Api.Configuration;
using MinhaApi.Api.Options;
using MinhaApi.CrossCutting.Logging;
using MinhaApi.CrossCutting.Middlewares;
using MinhaApi.IoC;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
builder.Host.UseSerilog(SerilogConfiguration.Configurar);

// 2. AddIoC()
builder.Services.AddIoC(builder.Configuration);

// Captive Dependency (README §7.6): valida em desenvolvimento que nenhum Singleton
// esta capturando algo Scoped (como o DbContext) - isso quebraria em producao de um
// jeito sutil, sem erro de compilacao.
builder.Host.UseDefaultServiceProvider((ctx, opts) =>
{
    opts.ValidateScopes = ctx.HostingEnvironment.IsDevelopment();
    opts.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
});

// 3. Options Pattern + ValidateOnStart()
builder.Services
    .AddOptions<MySqlOptions>()
    .Bind(builder.Configuration.GetSection("MySql"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 4. Controllers
// (validacao de FluentValidation e feita explicitamente na Application - ver
// ProdutoService - em vez de um filtro automatico de MVC)
// SuppressAsyncSuffixInActionNames = false: mantem o nome da action IGUAL ao nome
// do metodo (com "Async"), senao nameof(RecuperarAsync) no CreatedAtAction nao
// bate com o nome real da rota (que por padrao vira so "Recuperar") - "No route
// matches the supplied values".
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// 5. Swagger (registro)
builder.Services.AddSwaggerConfigurado();

// 6. CORS (origens explicitas - nunca AllowAnyOrigin em producao)
builder.Services.AddCorsConfigurada(builder.Configuration);

// 7. Rate Limiting
builder.Services.AddRateLimitConfigurado();

// 8. Health Checks
var connectionString = builder.Configuration.GetConnectionString("MySql");
builder.Services.AddHealthChecks()
    .AddMySql(connectionString ?? string.Empty, name: "mysql");

// 9. Limites do Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// 10. Authentication / Authorization -> FASE 17 (JWT)

var app = builder.Build();

// 11. CorrelationIdMiddleware
app.UseMiddleware<CorrelationIdMiddleware>();

// 12. ExceptionMiddleware
app.UseMiddleware<ExceptionMiddleware>();

// 13. SecurityHeadersMiddleware
app.UseMiddleware<SecurityHeadersMiddleware>();

// 14. UseHttpsRedirection + UseHsts (producao)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// 15. UseCors
app.UseCors(CorsConfiguration.PoliticaPadrao);

// 16. UseRateLimiter
app.UseRateLimiter();

// 17. UseAuthentication + UseAuthorization -> FASE 17 (JWT)

// 18. MapControllers + MapHealthChecks
app.MapControllers().RequireRateLimiting(RateLimitConfiguration.PoliticaPadrao);
app.MapHealthChecks("/health");

// 19. Swagger UI -> SOMENTE se IsDevelopment()
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 20. Graceful Shutdown
app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("Aplicacao esta parando - drenando requisicoes em andamento..."));

app.Run();
