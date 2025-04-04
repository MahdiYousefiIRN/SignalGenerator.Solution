using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Models
{
    public class IntegrityCheck
    {
        public DateTime CheckTime { get; set; }
        public bool IsValid { get; set; }
        public  string ValidationMessage { get; set; }
        public bool Success { get; internal set; }
    }
}
