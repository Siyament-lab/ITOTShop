using ITOTShop.Console.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ITOTShop.Console
    {
    internal class Program
        {
        static void Main ( string[] args )
            {
            // Skapar en host som hanterar konfiguration och dependency injection
            var builder = Host.CreateApplicationBuilder (args);

            // Hämtar connection string från appsettings.json
            var connectionString =
                builder.Configuration.GetConnectionString ("DefaultConnection");

            // Registrerar AppDbContext så att EF Core kan skapa det via DI
            builder.Services.AddDbContext<AppDbContext> (options =>
                options.UseSqlServer (connectionString));

            // Bygger applikationen
            var app = builder.Build ();

            app.Run ();
            }
        }
    }