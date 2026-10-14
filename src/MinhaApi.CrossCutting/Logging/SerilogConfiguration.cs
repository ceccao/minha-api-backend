using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace MinhaApi.CrossCutting.Logging;

public static class SerilogConfiguration
{
    // Chamado no Program.cs como: builder.Host.UseSerilog(SerilogConfiguration.Configurar);
    public static void Configurar(HostBuilderContext context, LoggerConfiguration loggerConfig)
    {
        loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId();

        if (context.HostingEnvironment.IsDevelopment())
        {
            loggerConfig
                .MinimumLevel.Debug()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            loggerConfig
                .MinimumLevel.Information()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(new JsonFormatter(), "logs/minha-api-.json", rollingInterval: RollingInterval.Day);
        }

        // TODO (LGPD - README §6.4): quando entidades com dados pessoais existirem
        // (CPF, email, telefone), adicionar uma politica de destructuring/masking
        // aqui antes de logar objetos completos. Ate la, nunca fazer log de entidade
        // crua - sempre projetar pra DTO antes de logar.
    }
}
