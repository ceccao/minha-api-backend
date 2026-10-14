using System.Reflection;
using Microsoft.OpenApi;

namespace MinhaApi.Api.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfigurado(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MinhaApi",
                Version = "v1"
            });

            // Le o arquivo XML gerado pelo GenerateDocumentationFile (csproj) e
            // usa os "/// <summary>" das controllers como descricao no Swagger.
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
