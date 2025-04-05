using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class ProtocolConfigs
    {
        public ModbusConfig Modbus { get; set; }
        public HttpConfig Http { get; set; }
        public SignalRConfig SignalR { get; set; }
    }
}
