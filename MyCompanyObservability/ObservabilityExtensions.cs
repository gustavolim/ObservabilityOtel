using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using System.Collections.Generic;
using System;

namespace MyCompany.Observability
{
    public static class ObservabilityExtensions
    {
        public static IServiceCollection AddMyCompanyObservability(this IServiceCollection services, IConfiguration config)
        {
            var serviceName = config["Observability:ServiceName"] ?? "myapp";
            var environment = config["Observability:Environment"] ?? "dev";
            var otlpEndpoint = config["Observability:OtlpEndpoint"] ?? "http://localhost:4317";

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("environment", environment),
                    new KeyValuePair<string, object>("version", "1.0.0")
                });

            services.AddOpenTelemetry()
                // TRACES
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = httpContext =>
                                !httpContext.Request.Path.StartsWithSegments("/healthcheck");
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(otlpEndpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        });
                })

                // METRICS
                .WithMetrics(builder =>
                {
                    builder
                        .SetResourceBuilder(resourceBuilder)
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(otlpEndpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        });
                });

            // LOGS
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.SetResourceBuilder(resourceBuilder);
                    options.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otlpEndpoint);
                        o.Protocol = OtlpExportProtocol.Grpc;
                    });
                });
            });

            // Loga as configurações na saída do console
            Console.WriteLine($"OpenTelemetry Config:");
            Console.WriteLine($"- Service: {serviceName}");
            Console.WriteLine($"- Environment: {environment}");
            Console.WriteLine($"- OTLP Endpoint: {otlpEndpoint}");

            return services;
        }
    }
}
