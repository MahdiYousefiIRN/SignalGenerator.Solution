using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly string _logFilePath;
        private readonly string _source;
        private readonly LogLevel _minLogLevel;
        private readonly List<Func<string, Task>> _logHandlers;
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Task _logProcessor;
        private bool _disposed = false;

        public LoggerHelper(
            string logFilePath = "logs.json",
            string source = "Application",
            LogLevel minLogLevel = LogLevel.Info)
        {
            _logFilePath = logFilePath;
            _source = source;
            _minLogLevel = minLogLevel;
            _logHandlers = new List<Func<string, Task>>();
            _logQueue = new ConcurrentQueue<string>();

            _logProcessor = Task.Run(ProcessLogQueue);
        }

        private void LogInternal(string message, LogLevel level, Exception? exception)
        {
            if (level < _minLogLevel) return;

            string logTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[{logTime}] [{_source}] [{level}] {message}";

            if (exception != null)
            {
                logMessage += $"\n🔴 Exception: {exception.Message}\n📌 StackTrace: {exception.StackTrace}";
            }

            _logQueue.Enqueue(logMessage);
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
                    // پردازش لاگ‌ها به صورت غیرهمزمان
                    var tasks = new List<Task>();

                    foreach (var handler in _logHandlers)
                    {
                        tasks.Add(handler(logMessage));
                    }

                    await Task.WhenAll(tasks);  // منتظر اتمام همه پردازش‌ها

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
                // استخراج سطح لاگ از پیام
                var level = ExtractLogLevel(logMessage);

                // تنظیم رنگ بر اساس سطح لاگ
                ConsoleColor color = level switch
                {
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Info => ConsoleColor.Green,
                    LogLevel.Debug => ConsoleColor.Cyan,
                    LogLevel.Trace => ConsoleColor.Gray,
                    _ => ConsoleColor.White
                };

                // نمایش پیام لاگ در کنسول
                Console.ForegroundColor = color;
                Console.WriteLine(logMessage);
                Console.ResetColor();

                await Task.CompletedTask;  // نیاز به انتظار برای عملیات کنسول نداریم
            });
        }

        private LogLevel ExtractLogLevel(string logMessage)
        {
            // استخراج سطح لاگ از پیام
            if (logMessage.Contains("ERROR")) return LogLevel.Error;
            if (logMessage.Contains("WARNING")) return LogLevel.Warning;
            if (logMessage.Contains("INFO")) return LogLevel.Info;
            if (logMessage.Contains("DEBUG")) return LogLevel.Debug;
            return LogLevel.Trace;
        }

        public void EnableFileLogging()
        {
            AddLogDestination(async logMessage =>
            {
                try
                {
                    await File.AppendAllTextAsync(_logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠ Error writing to log file: " + ex.Message);
                }
            });
        }

        private async Task WriteToEventLog(string logMessage)
        {
            try
            {
                if (!EventLog.SourceExists(_source))
                {
                    EventLog.CreateEventSource(_source, "Application");
                }

                using EventLog eventLog = new EventLog("Application") { Source = _source };
                eventLog.WriteEntry(logMessage, EventLogEntryType.Information);
                await Task.CompletedTask; // کار نوشتن به Event Log غیرهمزمان است
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠ Error writing to event log: " + ex.Message);
            }
        }

        public async Task EnableEventLogAsync()
        {
            AddLogDestination(async logMessage =>
            {
                await WriteToEventLog(logMessage);
            });
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public async Task LogError(string message, Exception ex)
        {
            await LogAsync(message, LogLevel.Error, ex);
        }
    }
}
