using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;
using ITOTShop.OrderConsole.Services;
using ITOTShop.OrderConsole.Helpers;

namespace ITOTShop.OrderConsole.Menu
    {
    public class AuthMenu
        {
        private readonly AuthService _authService;

        // Tar emot AuthService via dependency injection
        public AuthMenu ( AuthService authService )
            {
            _authService = authService;
            }

        // Startar huvudmenyn för autentisering
        public async Task<User?> RunAsync ()
            {
            while (true)
                {
                Console.WriteLine ("\n--- ITOTShop ---");
                Console.WriteLine ("1. Login");
                Console.WriteLine ("2. Create customer");
                Console.WriteLine ("0. Exit");
                Console.Write ("Choose option: ");

                var choice = Console.ReadLine ();

                switch (choice)
                    {
                    case "1":
                        var user = await LoginAsync ();

                        if (user != null)
                            {
                            return user;
                            }

                        break;

                    case "2":
                        var newUser = await CreateUserAsync ();

                        if (newUser != null)
                            {
                            return newUser;
                            }

                        break;

                    case "0":
                        return null;

                    default:
                        Console.WriteLine ("Invalid option.");
                        break;
                    }
                }
            }

        // Hanterar inloggning och låter användaren välja roll
        private async Task<User?> LoginAsync ()
            {
            while (true)
                {
                Console.WriteLine ("\n--- Login as ---");
                Console.WriteLine ("1. Customer");
                Console.WriteLine ("2. Admin");
                Console.WriteLine ("0. Back");
                Console.Write ("Choose role: ");

                var roleChoice = Console.ReadLine ();

                UserRole selectedRole;

                switch (roleChoice)
                    {
                    case "1":
                        selectedRole = UserRole.Customer;
                        break;

                    case "2":
                        selectedRole = UserRole.Admin;
                        break;

                    case "0":
                        return null;

                    default:
                        Console.WriteLine ("Invalid option.");
                        continue;
                    }

                Console.Write ("Username: ");
                var username = Console.ReadLine () ?? string.Empty;

                Console.Write ("Password: ");
                var password = SensitiveInput.ReadPassword ();

                // Kontrollerar användarnamn och lösenord
                var user = await _authService.LoginAsync (username, password);

                if (user == null)
                    {
                    Console.WriteLine ("Incorrect username or password.");
                    continue;
                    }

                // Kontrollerar att användaren har rollen som valdes vid login
                if (user.Role != selectedRole)
                    {
                    Console.WriteLine ("User does not have permission for the selected role.");
                    continue;
                    }

                Console.WriteLine ($"Welcome {user.Username}!");

                return user;
                }
            }

        // Skapar en ny användare med rollen Customer
        private async Task<User?> CreateUserAsync ()
            {
            Console.Write ("Username: ");
            var username = Console.ReadLine () ?? string.Empty;

            Console.Write ("Password: ");
            var password = SensitiveInput.ReadPassword ();

            try
                {
                // Vanliga registreringar får endast skapa Customer-konton
                var user = await _authService.RegisterAsync (
                    username,
                    password,
                    UserRole.Customer);

                Console.WriteLine ("Customer successfully created.");

                return user;
                }
            catch (ArgumentException ex)
                {
                Console.WriteLine ($"Could not create customer: {ex.Message}");
                return null;
                }
            }
        }
    }