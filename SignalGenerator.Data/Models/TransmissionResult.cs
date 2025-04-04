using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class TransmissionResult
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string ResponseMessage { get; set; }
        public int ResponseCode { get; set; }
    }
}
