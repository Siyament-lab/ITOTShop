using ITOTShop.Console.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ITOTShop.Console.Interfaces;
using ITOTShop.Console.DataAccess;
using ITOTShop.Console.Services;
using ITOTShop.Console.Menu;

namespace ITOTShop.Console
    {
    internal class Program
        {
        static async Task Main ( string[] args )
            {
            // Skapar en host som hanterar konfiguration och DI
            var builder = Host.CreateApplicationBuilder (args);

            // Hämtar connection string från appsettings.json
            var connectionString =
                builder.Configuration.GetConnectionString ("DefaultConnection");

            // Registrerar AppDbContext så att EF Core kan skapa det via DI
            builder.Services.AddDbContext<AppDbContext> (options =>
                options.UseSqlServer (connectionString));

            // Registrerar DataAccess-lagret.
            // När IOrderDataAccess efterfrågas används OrderDataAccess.
            builder.Services.AddScoped<IOrderDataAccess, OrderDataAccess> ();

            // Registrerar OrderService så att den kan hämtas via DI.
            builder.Services.AddScoped<OrderService> ();
            
            // Registrerar OrderMenu så att den kan hämtas .
            builder.Services.AddScoped<OrderMenu> ();
            
            // =====================Bygger applikationen==================================
            var app = builder.Build ();

            // Skapar ett scope för tjänster som använder AddScoped
            using var scope = app.Services.CreateScope ();

            // Hämtar databaskontexten från dependency injection
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext> ();

            // Lägger in en testkund om databasen saknar användare
            await UserSeed.SeedAsync (dbContext);

            // Hämtar och startar ordermenyn
            var orderMenu = scope.ServiceProvider.GetRequiredService<OrderMenu> ();

            

            await orderMenu.RunAsync ();

            
            }
        }
    }