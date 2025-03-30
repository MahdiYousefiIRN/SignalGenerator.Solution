namespace SignalGenerator.Data.Models
{
    public class SignalData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double Frequency { get; set; }   // فرکانس سیگنال
        public double Power { get; set; }       // توان سیگنال
        public DateTime Timestamp { get; set; } // زمان ارسال سیگنال
        public required string ProtocolType { get; set; }
        public bool CoilStatus { get; set; }
        public bool DiscreteInputStatus { get; set; }
    }
}
