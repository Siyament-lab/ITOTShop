using ITOTShop.OrderConsole.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.OrderConsole.Interfaces
    {
    public interface IUserDataAccess
        {
        //Hämta användare
        Task<User?> GetByUsernameAsync ( string username );

        //Hämta alla användare
        Task<List<User>> GetAllAsync ();

        //Lägg till ny användare
        Task AddAsync ( User user );

        //Kontroll om användarnamn finns
        Task<bool> UsernameExistsAsync(string username );
       
        }
    }
