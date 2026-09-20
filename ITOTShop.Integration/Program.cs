using ITOTShop.Integration.Data;
using ITOTShop.Integration.DataAccess;
using ITOTShop.Integration.Interfaces;
using ITOTShop.Integration.Services;
using ITOTShop.Integration.Monitoring;
using OpenTelemetry.Metrics;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.Integration
    {
    internal class Program
        {
        static void Main ( string[] args )
            {
            // Skapar Worker-applikationen och läser konfiguration från appsettings.json
            var builder = Host.CreateApplicationBuilder (args);

            // Hämtar connection string till den gemensamma orderdatabasen
            var connectionString =
                builder.Configuration.GetConnectionString ("DefaultConnection");

            // Registrerar IntegrationDbContext så integrationsprogrammet kan läsa Orders-tabellen
            builder.Services.AddDbContext<IntegrationDbContext> (options =>
                options.UseSqlServer (connectionString));

            // Registrerar ===OrderDataAccess==== som implementerar IOrderDataAccess
            builder.Services.AddScoped<IOrderDataAccess, OrderDataAccess> ();

            // Registrerar Worker som kör integrationsprocessen i bakgrunden
            builder.Services.AddHostedService<Worker> ();

            //Registrerar =========Modbusservice============== 
            builder.Services.AddScoped<ModbusService> ();

            //Registerar vår Metrics-service
            builder.Services.AddSingleton<IntegrationMetrics> ();

            // Konfigurerar OpenTelemetry och exponerar metrics för Prometheus.
            builder.Services
                .AddOpenTelemetry ()
                .WithMetrics (metrics =>
                {
                    metrics
                        // Lyssnar på alla värden som IntegrationMetrics skapar.
                        .AddMeter (IntegrationMetrics.MeterName)

                        .AddPrometheusHttpListener (options =>
                        {
                            // Exponerar metrics lokalt på datorn
                            options.Host = "localhost";
                            options.Port = 9464;
                        });
                });

            // Bygger och startar Worker Service
            var host = builder.Build ();
            host.Run ();
            }
        }
    }