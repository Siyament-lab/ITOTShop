using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;
using ITOTShop.OrderConsole.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ITOTShop.OrderConsole.Services
    {
    public class AuthService
        {
        private readonly IUserDataAccess _userDataAccess;
        private readonly IPasswordHasher<User> _passwordHasher;

        // Tar emot DataAccess och lösenordshashning via DI
        public AuthService (
            IUserDataAccess userDataAccess,
            IPasswordHasher<User> passwordHasher )
            {
            _userDataAccess = userDataAccess;
            _passwordHasher = passwordHasher;
            }

        // Kontrollerar om användarnamnet redan finns i databasen
        public async Task<bool> UserExistsAsync ( string username )
            {
            return await _userDataAccess.UsernameExistsAsync (username);
            }
        //Hämta alla användare
        public async Task<List<User>> GetAllUsersAsync ()
            {
            return await _userDataAccess.GetAllAsync ();
            }
        // Återställer lösenordet för en befintlig användare
        public async Task ResetPasswordAsync ( string username, string newPassword )
            {
            // Hämtar användaren från databasen
            var user = await _userDataAccess.GetByUsernameAsync (username);

            if (user == null)
                {
                throw new ArgumentException ("User does not exist.");
                }

            // Kontrollerar att det nya lösenordet inte är tomt
            if (string.IsNullOrWhiteSpace (newPassword))
                {
                throw new ArgumentException ("Password is required.");
                }

            // Skapar en ny hash från det nya lösenordet
            user.PasswordHash =
                _passwordHasher.HashPassword (user, newPassword);

            // Sparar den nya hashen i databasen
            await _userDataAccess.UpdateAsync (user);
            }

        // Skapar en ny användare med hashat lösenord
        public async Task<User> RegisterAsync (
            string username,
            string password,
            UserRole role )
            {
            // Kontrollerar att användarnamnet har ett värde
            if (string.IsNullOrWhiteSpace (username))
                {
                throw new ArgumentException ("Username is required.");
                }

            // Kontrollerar att lösenordet har ett värde
            if (string.IsNullOrWhiteSpace (password))
                {
                throw new ArgumentException ("Password is required.");
                }

            // Kontrollerar att användarnamnet inte redan finns
            if (await _userDataAccess.UsernameExistsAsync (username))
                {
                throw new ArgumentException ("Username already exists.");
                }

            var user = new User
                {
                Username = username,
                Role = role,
                CreatedAt = DateTime.UtcNow
                };

            // Hashar lösenordet innan användaren sparas i databasen
            user.PasswordHash =
                _passwordHasher.HashPassword (user, password);

            // Sparar användaren i databasen
            await _userDataAccess.AddAsync (user);

            return user;
            }

        // Kontrollerar användarnamn och lösenord vid inloggning
        public async Task<User?> LoginAsync (
            string username,
            string password )
            {
            // Hämtar användaren från databasen
            var user = await _userDataAccess.GetByUsernameAsync (username);

            // Avbryter om användaren inte finns
            if (user == null)
                {
                return null;
                }

            // Jämför angivet lösenord med den sparade hashen
            var result = _passwordHasher.VerifyHashedPassword (
                user,
                user.PasswordHash,
                password);

            // Returnerar null om lösenordet är fel
            if (result == PasswordVerificationResult.Failed)
                {
                return null;
                }

            // Returnerar användaren om inloggningen lyckades
            return user;
            }
        }
    }