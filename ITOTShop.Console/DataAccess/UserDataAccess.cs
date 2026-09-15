using ITOTShop.OrderConsole.Data;
using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITOTShop.OrderConsole.DataAccess
    {
    public class UserDataAccess: IUserDataAccess
        {
        private readonly AppDbContext _context;

        // Tar emot AppDbContext via DI
        public UserDataAccess ( AppDbContext context )
            {
            _context = context;
            }

        // Hämtar användaren som matchar angivet användarnamn
        public async Task<User?> GetByUsernameAsync ( string username )
            {
            return await _context.Users
                .FirstOrDefaultAsync (u => u.Username == username);
            }

        // Lägger till en ny användare och sparar ändringen i databasen
        public async Task AddAsync ( User user )
            {
            await _context.Users.AddAsync (user);
            await _context.SaveChangesAsync ();
            }

        // Returnerar true om användarnamnet redan finns
        public async Task<bool> UsernameExistsAsync ( string username )
            {
            return await _context.Users
                .AnyAsync (u => u.Username == username);
            }
        }
    }