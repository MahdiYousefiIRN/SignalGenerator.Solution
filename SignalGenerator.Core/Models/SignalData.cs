namespace SignalGenerator.Core.Models
{
    public class SignalData
    {
        public double Frequency { get; set; }   // فرکانس سیگنال
        public double Power { get; set; }       // توان سیگنال
        public DateTime Timestamp { get; set; } // زمان ارسال سیگنال
        public string ProtocolType { get; set; }
        public bool CoilStatus { get; set; }
        public bool DiscreteInputStatus { get; set; }
    }

}
