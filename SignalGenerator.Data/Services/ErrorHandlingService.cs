using SignalGenerator.Data.Services;
using SignalGenerator.Data.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Services
{
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ConcurrentDictionary<string, List<ErrorEvent>> _errorHistory;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHistory = new ConcurrentDictionary<string, List<ErrorEvent>>();
        }

        #region LogError and LogWarning

        public void LogError(string component, Exception ex, string? context = null)
        {
            if (string.IsNullOrEmpty(component))
                throw new ArgumentException("Component name cannot be null or empty", nameof(component));
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            LogEvent(component, ex.Message, ex.StackTrace, context, isWarning: false);
            _logger.LogError(ex, "Error in {Component}: {Message}", component, ex.Message);
        }

        public void LogWarning(string component, string message, string? context = null)
        {
            if (string.IsNullOrEmpty(component))
                throw new ArgumentException("Component name cannot be null or empty", nameof(component));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty", nameof(message));

            LogEvent(component, message, null, context, isWarning: true);
            _logger.LogWarning("Warning in {Component}: {Message}", component, message);
        }

        #endregion

        #region Private Helper Methods

        private void LogEvent(string component, string message, string? stackTrace, string? context, bool isWarning)
        {
            var errorEvent = new ErrorEvent
            {
                Timestamp = DateTime.UtcNow,
                Component = component,
                ErrorMessage = message,
                StackTrace = stackTrace ?? string.Empty,
                Context = context,
                IsWarning = isWarning
            };

            _errorHistory.AddOrUpdate(component, new List<ErrorEvent> { errorEvent }, (key, oldList) =>
            {
                oldList.Add(errorEvent);
                // Keep only the latest 1000 error events per component
                if (oldList.Count > 1000)
                    oldList.RemoveAt(0);
                return oldList;
            });
        }

        #endregion

        #region SystemStatus and Error Retrieval

        public SystemStatus GetSystemStatus()
        {
            var status = new SystemStatus
            {
                Timestamp = DateTime.UtcNow,
                ErrorCount = GetTotalErrorCount(),
                WarningCount = GetTotalWarningCount(),
                ComponentStatus = GetComponentStatus()
            };

            return status;
        }

        public async Task<List<ErrorEvent>> GetErrorsAsync(string? component = null, int count = 100)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            return await Task.Run(() =>
            {
                if (component != null)
                {
                    return _errorHistory.ContainsKey(component)
                        ? _errorHistory[component].OrderByDescending(e => e.Timestamp).Take(count).ToList()
                        : new List<ErrorEvent>();
                }

                return _errorHistory.Values
                    .SelectMany(e => e)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();
            });
        }

        #endregion

        #region Private Metrics Calculation

        private int GetTotalErrorCount()
        {
            return _errorHistory.Values
                .SelectMany(e => e)
                .Count(e => !e.IsWarning);
        }

        private int GetTotalWarningCount()
        {
            return _errorHistory.Values
                .SelectMany(e => e)
                .Count(e => e.IsWarning);
        }

        private Dictionary<string, ComponentStatus> GetComponentStatus()
        {
            return _errorHistory.ToDictionary(
                kvp => kvp.Key,
                kvp => new ComponentStatus
                {
                    ErrorCount = kvp.Value.Count(e => !e.IsWarning),
                    WarningCount = kvp.Value.Count(e => e.IsWarning),
                    LastError = kvp.Value.OrderByDescending(e => e.Timestamp).FirstOrDefault()
                }
            );
        }




        public async Task<List<ErrorEvent>> GetErrorsAsync(string? component = null, int count = 100, bool includeWarnings = false)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            // در اینجا از Task.Yield برای تحریک عملیات غیرهم‌زمان استفاده نمی‌کنیم، زیرا خود متد async است.

            // در صورتی که از ConcurrentDictionary استفاده می‌کنید، نیازی به قفل کردن نیست.
            var errorList = component != null
                ? _errorHistory.ContainsKey(component)
                    ? _errorHistory[component].OrderByDescending(e => e.Timestamp).Take(count).ToList()
                    : new List<ErrorEvent>()
                : _errorHistory.Values
                    .SelectMany(e => e)
                    .Where(e => includeWarnings || !e.IsWarning)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();

            return await Task.FromResult(errorList);  // اگر نیاز به اجرای یک عملیات غیرهم‌زمان دارید، از Task.FromResult برای برگرداندن نتیجه استفاده کنید.
        }


        #endregion
    }

    public class ErrorEvent
    {
        public DateTime Timestamp { get; set; }
        public string Component { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string? Context { get; set; }
        public bool IsWarning { get; set; }
    }

    public class SystemStatus
    {
        public DateTime Timestamp { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public Dictionary<string, ComponentStatus> ComponentStatus { get; set; } = new();
    }

    public class ComponentStatus
    {
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public ErrorEvent? LastError { get; set; }
    }
}
