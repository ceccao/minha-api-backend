using MinhaApi.Api.Configuration;
using MinhaApi.IoC;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigurarHost();                              // Serilog, Kestrel, validacao de escopo da DI
builder.Services.AddIoC(builder.Configuration);        // camadas de negocio e banco
builder.Services.AddApi(builder.Configuration, builder.Environment);     // controllers, swagger, cors, rate limit, health

var app = builder.Build();

app.UseApi();                                          // pipeline de middlewares
app.Run();
