using ITOTShop.Integration.Models;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.Integration.Data
    {
    public class IntegrationDbContext: DbContext
        {
        // Tar emot databasinställningar via dependency injection
        public IntegrationDbContext (
            DbContextOptions<IntegrationDbContext> options )
            : base (options)
            {
            }

        // Representerar den befintliga Orders-tabellen i databasen
        public DbSet<IntegrateOrder> Orders { get; set; }

        // Konfigurerar hur IntegrateOrder kopplas till den befintliga databastabellen
        protected override void OnModelCreating ( ModelBuilder modelBuilder )
            {
            base.OnModelCreating (modelBuilder);

            // IntegrateOrder ska använda Orders-tabellen som redan skapats av Console-projektet
            modelBuilder.Entity<IntegrateOrder> ()
                //Med "ToTable" mappar vi vår IntegrateOrder-klass till den befintliga Orders-tabellen i databasen
                .ToTable ("Orders");

            // Id är primärnyckel
            modelBuilder.Entity<IntegrateOrder> ()
                .HasKey (o => o.Id);

            // Matchar precisionen på UnitPrice i Orders-tabellen
            modelBuilder.Entity<IntegrateOrder> ()
                .Property (o => o.UnitPrice)
                .HasPrecision (18, 2);
            }
        }
    }