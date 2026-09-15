using ITOTShop.Integration.Data;
using ITOTShop.Integration.DataAccess;
using ITOTShop.Integration.Interfaces;
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

            // Bygger och startar Worker Service
            var host = builder.Build ();
            host.Run ();
            }
        }
    }