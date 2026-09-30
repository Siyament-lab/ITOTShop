using ITOTShop.Integration.Data;
using ITOTShop.Integration.DataAccess;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Metrics;
using ITOTShop.Integration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;

namespace ITOTShop.Integration
    {
    internal class Program
        {
        static void Main ( string[] args )
            {
            // Skapar applikationen med ASP.NET Core så Kestrel kan exponera /metrics.
            var builder = WebApplication.CreateBuilder (args);

            // Metrics-endpointen lyssnar på alla nätverksinterface på port 9464.
            // Detta gör att Prometheus i Docker kan nå Integration.
            builder.WebHost.UseUrls ("http://0.0.0.0:9464");

            // Hämtar connection string till den gemensamma orderdatabasen.
            var connectionString =
                builder.Configuration.GetConnectionString ("DefaultConnection");

            // Registrerar IntegrationDbContext så integrationsprogrammet kan läsa Orders-tabellen.
            builder.Services.AddDbContext<IntegrationDbContext> (options =>
                options.UseSqlServer (connectionString));

            // Registrerar OrderDataAccess som implementerar IOrderDataAccess.
            builder.Services.AddScoped<IOrderDataAccess, OrderDataAccess> ();

            // Registrerar Worker som kör integrationsprocessen i bakgrunden.
            builder.Services.AddHostedService<Worker> ();

            // Registrerar ModbusService.
            builder.Services.AddScoped<ModbusService> ();

            // Registrerar vår Metrics-service.
            builder.Services.AddSingleton<IntegrationMetrics> ();

            // Konfigurerar OpenTelemetry och Prometheus-exportern.
            builder.Services
                .AddOpenTelemetry ()
                .WithMetrics (metrics =>
                {
                    metrics
                        // Lyssnar på alla värden som IntegrationMetrics skapar.
                        .AddMeter (IntegrationMetrics.MeterName)

                        // Registrerar Prometheus-exportern.
                        .AddPrometheusExporter ();
                });

            // Bygger applikationen.
            var app = builder.Build ();

            // Exponerar metrics på:
            // http://localhost:9464/metrics
            app.MapPrometheusScrapingEndpoint ();

            // Startar både Worker och metrics-endpointen.
            app.Run ();
            }
        }
    }