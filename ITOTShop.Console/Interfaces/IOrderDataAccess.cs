using ITOTShop.OrderConsole.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.OrderConsole.Interfaces
    {
    public interface IOrderDataAccess
        {
        //Hämta alla ordrar från db
        Task<List<Order>> GetAllAsync ();

        //Lägger till ny order i db
        Task AddAsync ( Order order );
        }
    }
