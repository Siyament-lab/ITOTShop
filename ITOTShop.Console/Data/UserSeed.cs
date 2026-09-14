using ITOTShop.Console.Entities;
using ITOTShop.Console.Enums;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.Console.Data
    {
    public static class UserSeed
        {
        // Lägger till en testkund om det inte redan finns någon användare
        public static async Task SeedAsync ( AppDbContext context )
            {
            if (!await context.Users.AnyAsync ())
                {
                var user = new User
                    {
                    Username = "TestCustomer",

                    // Tillfälligt värde tills riktig lösenordshashning implementeras
                    PasswordHash = "temporary",

                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow
                    };

                context.Users.Add (user);

                await context.SaveChangesAsync ();
                }
            }
        }
    }