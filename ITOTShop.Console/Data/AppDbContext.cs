using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ITOTShop.Console.Entities;

namespace ITOTShop.Console.Data
    {
    public class AppDbContext : DbContext
        {
        public AppDbContext(DbContextOptions<AppDbContext> options ) 
            : base (options)
            {
            }
        //DbSets reperesenterar tabeller i databasen från entiteterna.
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }

        //Skriver över DbContext OnModelCreating metod för att lägg till min konfiguration för databasmodellen
        protected override void OnModelCreating ( ModelBuilder modelBuilder )
            {
            base.OnModelCreating (modelBuilder);

            //Här knyter vi ordrar till kund samt CUstomerId gör vi till forigenKey.
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u=> u.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete (DeleteBehavior.Restrict); // Kund med aktiva ordrar kan inte tas bort, för att undvika att ordrar blir "föräldralösa" i databasen.

            //Lagrar enhetspriser i SQL server med precision på 2 decimaler
            modelBuilder.Entity<Order>()
                .Property(o => o.UnitPrice)
                .HasColumnType ("decimal(18,2)");
            }
        }
    }
