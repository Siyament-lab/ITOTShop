using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ITOTShop.Console.Data
    {
    // Används av EF Core när migrationer skapas.
    // Skapar AppDbContext utan att applikationen behöver startas.
    public class AppDbContextFactory
        : IDesignTimeDbContextFactory<AppDbContext>
        {
        public AppDbContext CreateDbContext ( string[] args )
            {
            // Läser konfigurationen från appsettings.json
            var configuration = new ConfigurationBuilder ()
                .SetBasePath (Directory.GetCurrentDirectory ())
                .AddJsonFile ("appsettings.json")
                .Build ();

            // Hämtar connection string från appsettings.json
            var connectionString =
                configuration.GetConnectionString ("DefaultConnection");

            // Konfigurerar AppDbContext för SQL Server
            var optionsBuilder =
                new DbContextOptionsBuilder<AppDbContext> ();

            optionsBuilder.UseSqlServer (connectionString);

            // Returnerar ett färdigkonfigurerat AppDbContext
            return new AppDbContext (optionsBuilder.Options);
            }
        }
    }