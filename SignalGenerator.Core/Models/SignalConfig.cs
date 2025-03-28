namespace SignalGenerator.Core.Models
{
    public class SignalConfig
    {
        public int SignalCount { get; set; }    // تعداد سیگنال‌ها
        public double MinFrequency { get; set; } // حداقل فرکانس
        public double MaxFrequency { get; set; } // حداکثر فرکانس
        public int IntervalMs { get; set; }     // فاصله زمانی ارسال سیگنال
    }

}
