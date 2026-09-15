namespace ITOTShop.OrderConsole.Helpers
    {
    public static class SensitiveInput
        {
        // Läser in känslig information utan att visa de riktiga tecknen i konsolen
        public static string ReadPassword ()
            {
            string password = string.Empty;

            while (true)
                {
                var key = Console.ReadKey (intercept: true);

                if (key.Key == ConsoleKey.Enter)
                    {
                    Console.WriteLine ();
                    break;
                    }

                if (key.Key == ConsoleKey.Backspace)
                    {
                    if (password.Length > 0)
                        {
                        password = password[..^1];
                        Console.Write ("\b \b");
                        }

                    continue;
                    }

                password += key.KeyChar;
                Console.Write ("*");
                }

            return password;
            }
        }
    }