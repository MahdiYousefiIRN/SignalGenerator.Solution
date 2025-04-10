using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

namespace SignalGenerator.Helpers
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILoggerService
    {
        Task LogAsync(string message, LogLevel level = LogLevel.Info, Exception? exception = null);
        Task LogInfo(string message);
        Task LogWarning(string message);
        Task LogError(string message);
        Task LogError(string message, Exception ex);
        Task LogCritical(string message);
    }

    public class LoggerHelper : ILoggerService, IDisposable
    {
        private readonly string _logDirectory;
        private readonly string _source;
        private readonly LogLevel _minLogLevel;
        private readonly List<Func<string, Task>> _logHandlers;
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Task _logProcessor;
        private bool _disposed = false;
        private DateTime _lastLogTime;
        private string _currentLogFilePath;
        private string _currentLogDirectory;

        public LoggerHelper(
            string logDirectory = "Logs", // پوشه ای برای ذخیره لاگ ها
            string source = "SignalGeneratorApp", // نام منبع لاگ ها
            LogLevel minLogLevel = LogLevel.Info) // حداقل سطح لاگ
        {
            _logDirectory = logDirectory;
            _source = source;
            _minLogLevel = minLogLevel;
            _logHandlers = new List<Func<string, Task>>();
            _logQueue = new ConcurrentQueue<string>();

            // ایجاد پوشه برای ذخیره لاگ‌ها در صورت عدم وجود
            Directory.CreateDirectory(_logDirectory);

            // فرآیند پردازش لاگ‌ها
            _logProcessor = Task.Run(ProcessLogQueue);
            _lastLogTime = DateTime.UtcNow;

            // تاریخ شمسی برای نامگذاری پوشه‌ها
            PersianCalendar persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(DateTime.UtcNow);
            int month = persianCalendar.GetMonth(DateTime.UtcNow);
            int day = persianCalendar.GetDayOfMonth(DateTime.UtcNow);
            _currentLogDirectory = Path.Combine(_logDirectory, $"{year}-{month:D2}-{day:D2}");
            Directory.CreateDirectory(_currentLogDirectory);

            _currentLogFilePath = GenerateLogFilePath(); // تولید مسیر فایل لاگ جدید
        }

        private string GenerateLogFilePath()
        {
            // تاریخ شمسی برای نامگذاری فایل
            PersianCalendar persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(DateTime.UtcNow);
            int month = persianCalendar.GetMonth(DateTime.UtcNow);
            int day = persianCalendar.GetDayOfMonth(DateTime.UtcNow);
            int hour = persianCalendar.GetHour(DateTime.UtcNow);
            int minute = persianCalendar.GetMinute(DateTime.UtcNow);

            // تولید نام فایل لاگ با تاریخ و زمان شمسی
            string timeStamp = $"{year}-{month:D2}-{day:D2}_{hour:D2}{minute:D2}";
            return Path.Combine(_currentLogDirectory, $"log_{timeStamp}.txt");
        }

        private void LogInternal(string message, LogLevel level, Exception? exception)
        {
            if (level < _minLogLevel) return;

            // تاریخ و زمان شمسی برای لاگ
            PersianCalendar persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(DateTime.UtcNow);
            int month = persianCalendar.GetMonth(DateTime.UtcNow);
            int day = persianCalendar.GetDayOfMonth(DateTime.UtcNow);
            string logTime = $"{year}-{month:D2}-{day:D2} {persianCalendar.GetHour(DateTime.UtcNow)}:{persianCalendar.GetMinute(DateTime.UtcNow):D2}";

            string logMessage = $"[{logTime}] [{_source}] [{level}] {message}";

            if (exception != null)
            {
                logMessage += $"\n🔴 Exception: {exception.Message}\n📌 StackTrace: {exception.StackTrace}";
            }

            _logQueue.Enqueue(logMessage);

            // بررسی زمان برای تغییر فایل لاگ
            if ((DateTime.UtcNow - _lastLogTime).TotalMinutes >= 1)
            {
                // اگر بیشتر از یک دقیقه از زمان آخرین لاگ گذشته باشد، فایل جدید ایجاد می‌شود
                _currentLogFilePath = GenerateLogFilePath();
                _lastLogTime = DateTime.UtcNow;
            }
        }

        public async Task LogAsync(string message, LogLevel level = LogLevel.Info, Exception? exception = null)
        {
            await Task.Yield();  // برای جلوگیری از بلاک شدن عملیات
            LogInternal(message, level, exception);
        }

        public async Task LogInfo(string message)
        {
            await LogAsync(message, LogLevel.Info);
        }

        public async Task LogWarning(string message)
        {
            await LogAsync(message, LogLevel.Warning);
        }

        public async Task LogError(string message)
        {
            await LogAsync(message, LogLevel.Error);
        }

        public async Task LogError(string message, Exception ex)
        {
            await LogAsync(message, LogLevel.Error, ex);
        }

        public async Task LogCritical(string message)
        {
            await LogAsync(message, LogLevel.Critical);
        }

        private async Task ProcessLogQueue()
        {
            while (!_disposed)
            {
                while (_logQueue.TryDequeue(out var logMessage))
                {
                    // ارسال لاگ به هر مقصد (مانند کنسول یا فایل)
                    var tasks = new List<Task>();

                    foreach (var handler in _logHandlers)
                    {
                        tasks.Add(handler(logMessage));
                    }

                    await Task.WhenAll(tasks);  // منتظر اتمام همه پردازش‌ها

                    // ذخیره لاگ به فایل
                    try
                    {
                        await File.AppendAllTextAsync(_currentLogFilePath, logMessage + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("⚠ Error writing to log file: " + ex.Message);
                    }

                    await Task.Delay(50); // جلوگیری از مصرف بیش از حد منابع
                }

                await Task.Delay(200);  // به صورت غیرهمزمان تا 200 میلی‌ثانیه صبر می‌کنیم
            }
        }

        public void AddLogDestination(Func<string, Task> logHandler)
        {
            _logHandlers.Add(logHandler);
        }

        public void EnableConsoleLogging()
        {
            AddLogDestination(async logMessage =>
            {
                // نمایش پیام لاگ در کنسول
                Console.WriteLine(logMessage);
                await Task.CompletedTask;
            });
        }

        public void EnableFileLogging()
        {
            // این متد دیگر نیازی به تغییر ندارد چرا که لاگ‌ها به‌طور پیش‌فرض در فایل ذخیره می‌شوند.
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
