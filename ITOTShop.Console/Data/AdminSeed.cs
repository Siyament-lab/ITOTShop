using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.OrderConsole.Data
    {
    public static class AdminSeed
        {
        // Skapar en admin-användare om ingen admin redan finns
        public static async Task SeedAsync (
            AppDbContext context,
            IPasswordHasher<User> passwordHasher )
            {
            bool adminExists = await context.Users
                .AnyAsync (u => u.Role == UserRole.Admin);

            if (adminExists)
                {
                return;
                }

            var admin = new User
                {
                Username = "Admin",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
                };

            // Hashar admin-lösenordet innan användaren sparas
            admin.PasswordHash =
                passwordHasher.HashPassword (admin, "Admin123!");

            context.Users.Add (admin);

            await context.SaveChangesAsync ();
            }
        }
    }