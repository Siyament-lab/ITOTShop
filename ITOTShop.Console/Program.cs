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

            //Registrerar menyn som hanterar roller och användare
            builder.Services.AddScoped<RoleMenu> ();

            //Reigstrerar AdminMenu så att den kan hämtas via DI
            builder.Services.AddScoped<AdminMenu> ();

            // =====================Bygger applikationen==================================
            var app = builder.Build ();

            // Skapar ett scope för tjänster som använder AddScoped
            using var scope = app.Services.CreateScope ();

            // Hämtar databaskontexten från DI
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext> ();

            // Hämtar tjänsten som används för lösenordshashning
            var passwordHasher =
                scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>> ();

            // Skapar en admin om ingen admin redan finns
            await AdminSeed.SeedAsync (dbContext, passwordHasher);

            // Hämtar autentiseringsmenyn
            var authMenu = scope.ServiceProvider.GetRequiredService<AuthMenu> ();

            // Hämtar rollmenyn
            var roleMenu = scope.ServiceProvider.GetRequiredService<RoleMenu> ();

            // Håller programmet igång tills användaren väljer att avsluta
            while (true)
                {
                // Startar login och hämtar den inloggade användaren
                var currentUser = await authMenu.RunAsync ();

                // Avslutar hela programmet om användaren väljer Exit
                if (currentUser == null)
                    {
                    Console.WriteLine ("Application closed.");
                    break;
                    }

                // Startar rätt meny beroende på användarens roll.
                // När användaren loggar ut kommer programmet tillbaka hit.
                await roleMenu.RunAsync (currentUser);

                }
            }
        }
    }