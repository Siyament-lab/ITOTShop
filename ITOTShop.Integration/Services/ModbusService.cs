using NModbus;
using System.Net.Sockets;
using ITOTShop.Integration.DTOs;
using ITOTShop.Integration.Enums;

namespace ITOTShop.Integration.Services
    {
    public class ModbusService
        {
        private readonly string _ipAddress = "127.0.0.1";
        private readonly int _port = 5020;
        private readonly byte _slaveId = 1;



        // Data som C# skickar till PLC
        private const ushort OrderIdAddress = 0;              // %QW0
        private const ushort RequestedQuantityAddress = 1;   // %QW1
        private const ushort StartProductionAddress = 0;     // %QX0.0
        private const ushort ResetProductionAddress = 1;    //%QX0.1

        // Data som C# läser från PLC
        private const ushort ProductionStatusAddress = 2;    // %QW2
        private const ushort ProducedQuantityAddress = 3;    // %QW3
        private const ushort FaultCodeAddress = 4;           // %QW4

        private const ushort ProductionCompleteAddress = 2;  // %QX0.2
        private const ushort ProductionFaultAddress = 3;     // %QX0.3

        // ======================== TEST AV PLC ========================
        //public async Task SendTestOrderAsync ()
        //    {
        //    // Testvärden innan vi kopplar in riktiga ordrar från databasen
        //    ushort orderId = 99;
        //    ushort requestedQuantity = 5;

        //    // Skapar TCP-klient
        //    using var tcpClient = new TcpClient ();

        //    // Ansluter till OpenPLC Runtime på port 5020
        //    await tcpClient.ConnectAsync (
        //        _ipAddress,
        //        _port);

        //    // Skapar Modbus Master
        //    var factory = new ModbusFactory ();

        //    var master = factory.CreateMaster (
        //        tcpClient);

        //    // -------------------------------------------------------
        //    // 1. SKRIVER ORDER-ID TILL OPENPLC
        //    // %QW0
        //    // -------------------------------------------------------
        //    master.WriteSingleRegister (
        //        _slaveId,
        //        OrderIdAddress,
        //        orderId);

        //    // -------------------------------------------------------
        //    // 2. SKRIVER ANTAL TILL OPENPLC
        //    // %QW1
        //    // -------------------------------------------------------
        //    master.WriteSingleRegister (
        //        _slaveId,
        //        RequestedQuantityAddress,
        //        requestedQuantity);

        //    // -------------------------------------------------------
        //    // 3. SKICKAR STARTSIGNAL
        //    // %QX0.0
        //    // -------------------------------------------------------
        //    master.WriteSingleCoil (
        //        _slaveId,
        //        StartProductionAddress,
        //        true);

        //    Console.WriteLine (
        //        $"Test order sent: OrderId={orderId}, Quantity={requestedQuantity}");

        //    // -------------------------------------------------------
        //    // 4. LÄSER TILLBAKA %QW0 OCH %QW1
        //    // Detta kontrollerar att OpenPLC verkligen tog emot värdena
        //    // -------------------------------------------------------
        //    var testRegisters = master.ReadHoldingRegisters (
        //        _slaveId,
        //        OrderIdAddress,
        //        2);

        //    Console.WriteLine (
        //        $"PLC readback: OrderId={testRegisters[0]}, Quantity={testRegisters[1]}");

        //    // Håller startsignalen TRUE i 5 sekunder
        //    // så att vi hinner se den i OpenPLC Debug
        //    await Task.Delay (5000);

        //    // -------------------------------------------------------
        //    // 5. SLÄPPER STARTSIGNALEN
        //    // %QX0.0 går tillbaka till FALSE
        //    // -------------------------------------------------------
        //    master.WriteSingleCoil (
        //        _slaveId,
        //        StartProductionAddress,
        //        false);
        //    }
        ////=============================TEST slut=============================


        // ==================== PERMANENT PROGRAM ====================

        //Skickar order-data till PLC
        public async Task SendOrderAsync (
            ushort orderId,
            ushort requestedQuantity )
            {
            using var tcpClient = new TcpClient ();

            await tcpClient.ConnectAsync (
                _ipAddress,
                _port);

            var factory = new ModbusFactory ();

            var master = factory.CreateMaster (
                tcpClient);

            // %QW0 - OrderId
            master.WriteSingleRegister (
                _slaveId,
                OrderIdAddress,
                orderId);

            // %QW1 - RequestedQuantity
            master.WriteSingleRegister (
                _slaveId,
                RequestedQuantityAddress,
                requestedQuantity);

            // %QX0.0 - StartProduction
            master.WriteSingleCoil (
                _slaveId,
                StartProductionAddress,
                true);

            // Startsignal till PLC:n 1s för att den ska hinna läsa
            await Task.Delay (1000);

            master.WriteSingleCoil (
                _slaveId,
                StartProductionAddress,
                false);
            }

        //SKickar återställning av produktion från PLC
        public async Task ResetProductionAsync ()
            {
            using var tcpClient = new TcpClient ();

            // Ansluter till OpenPLC Runtime
            await tcpClient.ConnectAsync (
                _ipAddress,
                _port);

            var factory = new ModbusFactory ();
            var master = factory.CreateMaster (tcpClient);

            // Skickar resetsignal till %QX0.1
            master.WriteSingleCoil (
                _slaveId,
                ResetProductionAddress,
                true);

            // Kort signal så PLC:n hinner registrera reset
            await Task.Delay (5000);

            // Släpper resetsignalen igen
            master.WriteSingleCoil (
                _slaveId,
                ResetProductionAddress,
                false);
            }

        //Läser produktionsstatus från PLC
        public ProductionStateDto ReadProductionState ()
            {
            using var tcpClient = new TcpClient (
                _ipAddress,
                _port);

            var factory = new ModbusFactory ();
            var master = factory.CreateMaster (tcpClient);

            // Läser %QW2, %QW3 och %QW4
            var registers = master.ReadHoldingRegisters (
                _slaveId,
                ProductionStatusAddress,
                3);

            // Läser %QX0.2 och %QX0.3
            var coils = master.ReadCoils (
                _slaveId,
                ProductionCompleteAddress,
                2);

            return new ProductionStateDto
                {
                Status = (ProductionStatus) registers[0],
                ProducedQuantity = registers[1],
                FaultCode = registers[2],

                ProductionComplete = coils[0],
                ProductionFault = coils[1]
                };
            }
        
        }
    }