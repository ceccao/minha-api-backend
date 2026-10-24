using MinhaApi.Api.Configuration;
using MinhaApi.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigurarHost();                                              // Serilog, Kestrel, validacao de escopo da DI
builder.Services.AddIoC(builder.Configuration);                        // camadas de negocio, banco e autenticacao
builder.Services.AddApi(builder.Configuration, builder.Environment);   // controllers, swagger, cors, rate limit, jwt, health

var app = builder.Build();

app.UseApi();                                                          // pipeline de middlewares
await app.SeedAdminAsync();                                            // primeiro usuario, se configurado
app.Run();
