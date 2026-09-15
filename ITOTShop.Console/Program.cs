using ITOTShop.OrderConsole.Data;
using ITOTShop.OrderConsole.DataAccess;
using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Interfaces;
using ITOTShop.OrderConsole.Menu;
using ITOTShop.OrderConsole.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ITOTShop.OrderConsole
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
            //====================Registrerar användarhantering==================================
            // Registrerar DataAccess för användarhantering
            builder.Services.AddScoped<IUserDataAccess, UserDataAccess> ();

            // Registrerar tjänsten som hash-ar och verifierar lösenord
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>> ();

            // Registrerar autentiseringstjänsten
            builder.Services.AddScoped<AuthService> ();

            // Registrerar AuthMenu så att den kan hämtas via DI
            builder.Services.AddScoped<AuthMenu> ();

            // =====================Bygger applikationen==================================
            var app = builder.Build ();

            // Skapar ett scope för tjänster som använder AddScoped
            using var scope = app.Services.CreateScope ();

            // Hämtar autentiseringsmenyn
            var authMenu = scope.ServiceProvider.GetRequiredService<AuthMenu> ();

            // Startar login och hämtar den inloggade användaren
            var currentUser = await authMenu.LoginAsync ();

            // Avslutar programmet om användaren avbryter inloggningen
            if (currentUser == null)
                {
                Console.WriteLine ("Login cancelled.");
                return;
                }

            // Hämtar ordermenyn
            var orderMenu = scope.ServiceProvider.GetRequiredService<OrderMenu> ();

            // Startar ordermenyn med den inloggade användaren
            await orderMenu.RunAsync (currentUser);

            }
        }
    }