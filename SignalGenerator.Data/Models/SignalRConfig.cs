using SignalGenerator.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class SignalRConfig : IProtocolConfig
    {
        public string IpAddress { get; set; } = "localhost";
        public int Port { get; set; } = 5000;
        public string HubUrl { get; set; } = "/signalhub";
        public int KeepAliveSeconds { get; set; } = 30;
    }
}
