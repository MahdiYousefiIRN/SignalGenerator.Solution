using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class ErrorDetail
    {
        public DateTime Timestamp { get; set; }
        public required string ErrorMessage { get; set; }
        public required string ErrorSource { get; set; }
    }
}
