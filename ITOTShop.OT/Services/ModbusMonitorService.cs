using ITOTShop.OT.Models;
using NModbus;
using System.Net.Sockets;

namespace ITOTShop.OT.Services
    {
    public class ModbusMonitorService: IDisposable
        {
        private readonly string _ipAddress = "127.0.0.1";
        private readonly int _port = 5020;
        private readonly byte _slaveId = 1;

        private TcpClient? _tcpClient;
        private IModbusMaster? _master;


        // OpenPLC Holding Registers
        private const ushort OrderIdAddress = 0;              // %QW0
        private const ushort RequestedQuantityAddress = 1;   // %QW1
        private const ushort ProductionStatusAddress = 2;    // %QW2
        private const ushort ProducedQuantityAddress = 3;    // %QW3
        private const ushort FaultCodeAddress = 4;            // %QW4


        // OpenPLC Coils
        private const ushort ProductionCompleteAddress = 2;  // %QX0.2
        private const ushort ProductionFaultAddress = 3;     // %QX0.3


        // Skapar Modbus-anslutningen endast när det behövs.
        // Samma TCP-anslutning återanvänds mellan avläsningarna.
        private void EnsureConnected ()
            {
            if (_tcpClient != null &&
                _tcpClient.Connected &&
                _master != null)
                {
                return;
                }


            // Städar eventuell gammal anslutning
            DisposeConnection ();


            // Skapar en ny anslutning mot OpenPLC Runtime
            _tcpClient = new TcpClient ();

            _tcpClient.Connect (
                _ipAddress,
                _port);


            var factory =
                new ModbusFactory ();


            _master =
                factory.CreateMaster (
                    _tcpClient);
            }


        // Läser aktuell produktionsstatus direkt från OpenPLC.
        public ProductionState ReadProductionState ()
            {
            try
                {
                // Säkerställer att anslutningen finns
                EnsureConnected ();


                // Läser %QW0 till %QW4
                var registers =
                    _master!.ReadHoldingRegisters (
                        _slaveId,
                        OrderIdAddress,
                        5);


                // Läser %QX0.2 och %QX0.3
                var coils =
                    _master.ReadCoils (
                        _slaveId,
                        ProductionCompleteAddress,
                        2);


                return new ProductionState
                    {
                    OrderId = registers[0],

                    RequestedQuantity = registers[1],

                    ProductionStatus = registers[2],

                    ProducedQuantity = registers[3],

                    FaultCode = registers[4],

                    ProductionComplete = coils[0],

                    ProductionFault = coils[1]
                    };
                }
            catch
                {
                // Om anslutningen bryts stängs den gamla anslutningen.
                // Vid nästa avläsning försöker OT-programmet ansluta igen.
                DisposeConnection ();

                throw;
                }
            }


        // Stänger den aktuella Modbus/TCP-anslutningen.
        private void DisposeConnection ()
            {
            _master?.Dispose ();
            _master = null;

            _tcpClient?.Dispose ();
            _tcpClient = null;
            }


        // Körs när ModbusMonitorService avslutas.
        public void Dispose ()
            {
            DisposeConnection ();

            GC.SuppressFinalize (this);
            }
        }
    }