using SignalGenerator.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class HttpConfig : IProtocolConfig
    {
        public string IpAddress { get; set; } = "localhost";
        public int Port { get; set; } = 5001;
        public string BasePath { get; set; } = "/api/signals";
    }

}
