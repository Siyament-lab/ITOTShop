using ITOTShop.OrderConsole.Entities;
using ITOTShop.OrderConsole.Enums;

namespace ITOTShop.OrderConsole.Menu
    {
    public class RoleMenu
        {
        private readonly OrderMenu _orderMenu;
        private readonly AdminMenu _adminMenu;

        // Tar emot de menyer som kan användas beroende på användarens roll
        public RoleMenu ( OrderMenu orderMenu, AdminMenu adminMenu )
            {
            _orderMenu = orderMenu;
            _adminMenu = adminMenu;
            }

        // Kontrollerar användarens roll och skickar vidare till rätt meny
        public async Task RunAsync ( User currentUser )
            {
            switch (currentUser.Role)
                {
                case UserRole.Customer:
                    await _orderMenu.RunAsync (currentUser);
                    break;

                case UserRole.Admin:
                    await _adminMenu.RunAsync ();
                    break;

                default:
                    Console.WriteLine ("Unknown user role.");
                    break;
                }
            }
        }
    }