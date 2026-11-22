using System.Globalization;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace MinhaApi.CrossCutting.Logging;

public static class SerilogConfiguration
{
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
                .WriteTo.File(
                    new JsonFormatter(),
                    "logs/minha-api-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14);
        }
    }
}
