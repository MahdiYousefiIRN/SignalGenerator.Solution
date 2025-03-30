namespace SignalGenerator.Data.Models
{
    public class SignalStatus
    {
        public bool IsGenerating { get; set; }
        public int GeneratedSignalCount { get; set; }
        public DateTime LastSignalGenerated { get; set; }
        public string? LastError { get; set; }
    }

}
