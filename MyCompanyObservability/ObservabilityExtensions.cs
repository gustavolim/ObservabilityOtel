using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using System;
using OpenTelemetry.Logs;
using OpenTelemetry.Extensions.Hosting;

namespace MyCompany.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddMyCompanyObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["Observability:ServiceName"] ?? "MyService";
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317";
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

        // Adiciona OpenTelemetry para Tracing e Metrics
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint))
            )
            .WithMetrics(metrics => metrics
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint))
            );

        // Adiciona OpenTelemetry para Logging
        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resourceBuilder);
                logging.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
            });
        });

        return services;
    }
}