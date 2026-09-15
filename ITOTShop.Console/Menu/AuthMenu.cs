using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;
using ITOTShop.OrderConsole.Services;

namespace ITOTShop.OrderConsole.Menu
    {
    public class AuthMenu
        {
        private readonly AuthService _authService;

        // Tar emot AuthService via DI
        public AuthMenu ( AuthService authService )
            {
            _authService = authService;
            }

        // Hanterar inloggning om anv. inte finns får den möjlighet att skapa en ny anv.
        public async Task<User?> LoginAsync ()
            {
            Console.Write ("Username: ");
            var username = Console.ReadLine () ?? string.Empty;

            // Kontrollerar först om användaren finns
            if (!await _authService.UserExistsAsync (username))
                {
                Console.WriteLine ("User does not exist. Choose an option:");
                Console.WriteLine ("1. Create new user");
                Console.WriteLine ("0. Cancel");
               //Console.Write ("Choose option: ");

                var choice = Console.ReadLine ();

                if (choice == "1")
                    {
                    return await CreateUserAsync (username);
                    }

                // Avbryter och skickar tillbaka null
                return null;
                }

            Console.Write ("Password: ");
            Console.Write ("Password: ");
            var password = ReadPassword ();

            // Försöker logga in användaren
            var user = await _authService.LoginAsync (username, password);

            if (user == null)
                {
                Console.WriteLine ("Incorrect password.");
                return null;
                }

            Console.WriteLine ($"Welcome {user.Username}!");

            return user;
            }

        // Skapar en ny kund med det användarnamn som redan angivits
        private async Task<User?> CreateUserAsync ( string username )
            {
            Console.Write ("Choose password: ");
            var password = Console.ReadLine () ?? string.Empty;

            try
                {
                // Nya användare som registrerar sig själva skapas som Customer
                var user = await _authService.RegisterAsync (
                    username,
                    password,
                    UserRole.Customer);

                Console.WriteLine ("User successfully created.");

                return user;
                }
            catch (ArgumentException ex)
                {
                Console.WriteLine ($"Could not create user: {ex.Message}");
                return null;
                }
            }
        //Metod för att dölj lösenordet när användaren skriver in det
        private string ReadPassword ()
            {
            string password = string.Empty;

            while (true)
                {
                var key = Console.ReadKey (intercept: true);

                // Enter avslutar inmatningen
                if (key.Key == ConsoleKey.Enter)
                    {
                    Console.WriteLine ();
                    break;
                    }

                // Backspace tar bort senaste tecknet
                if (key.Key == ConsoleKey.Backspace)
                    {
                    if (password.Length > 0)
                        {
                        password = password[..^1];

                        // Tar bort senaste stjärnan från konsolen
                        Console.Write ("\b \b");
                        }

                    continue;
                    }

                // Sparar tecknet men visar endast en stjärna
                password += key.KeyChar;
                Console.Write ("*");
                }

            return password;
            }
        }
    }