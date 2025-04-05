using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolConfig
    {
        string IpAddress { get; }
        int Port { get; }
    }

}
