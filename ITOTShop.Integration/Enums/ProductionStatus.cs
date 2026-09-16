using System;
using System.Collections.Generic;
using System.Text;

namespace ITOTShop.Integration.Enums
    {
    public enum ProductionStatus
        {
        //Status Variabler från PLC variabler
        Idle = 0,
        Processing = 1,
        Completed = 2,
        Fault = 3
        }
    }
