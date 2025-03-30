using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace SignalGenerator.Data.Services
{
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly Dictionary<string, List<ErrorEvent>> _errorHistory;
        private readonly object _lockObject = new object();
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _memoryCounter;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHistory = new Dictionary<string, List<ErrorEvent>>();

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize performance counters. System status monitoring will be limited.");
                }
            }
            else
            {
                _logger.LogInformation("Performance counters are only supported on Windows. System status monitoring will be limited.");
            }
        }

        public void LogError(string component, Exception ex, string? context = null)
        {
            if (string.IsNullOrEmpty(component))
                throw new ArgumentException("Component name cannot be null or empty", nameof(component));
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            var errorEvent = new ErrorEvent
            {
                Timestamp = DateTime.UtcNow,
                Component = component,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty,
                Context = context
            };

            lock (_lockObject)
            {
                if (!_errorHistory.ContainsKey(component))
                {
                    _errorHistory[component] = new List<ErrorEvent>();
                }
                _errorHistory[component].Add(errorEvent);

                // Keep only last 1000 errors per component
                if (_errorHistory[component].Count > 1000)
                {
                    _errorHistory[component].RemoveAt(0);
                }
            }

            _logger.LogError(ex, "Error in {Component}: {Message}", component, ex.Message);
        }

        public void LogWarning(string component, string message, string? context = null)
        {
            if (string.IsNullOrEmpty(component))
                throw new ArgumentException("Component name cannot be null or empty", nameof(component));
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message cannot be null or empty", nameof(message));

            var warningEvent = new ErrorEvent
            {
                Timestamp = DateTime.UtcNow,
                Component = component,
                ErrorMessage = message,
                Context = context,
                IsWarning = true
            };

            lock (_lockObject)
            {
                if (!_errorHistory.ContainsKey(component))
                {
                    _errorHistory[component] = new List<ErrorEvent>();
                }
                _errorHistory[component].Add(warningEvent);
            }

            _logger.LogWarning("Warning in {Component}: {Message}", component, message);
        }

        public SystemStatus GetSystemStatus()
        {
            try
            {
                var status = new SystemStatus
                {
                    Timestamp = DateTime.UtcNow,
                    ErrorCount = GetTotalErrorCount(),
                    WarningCount = GetTotalWarningCount(),
                    ComponentStatus = GetComponentStatus()
                };

                if (OperatingSystem.IsWindows() && _cpuCounter != null && _memoryCounter != null)
                {
                    try
                    {
                        status.CpuUsage = _cpuCounter.NextValue();
                        status.AvailableMemory = _memoryCounter.NextValue();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read performance counters");
                    }
                }
                else
                {
                    // Use alternative methods or set default values for non-Windows platforms
                    status.CpuUsage = -1;
                    status.AvailableMemory = -1;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return new SystemStatus
                {
                    Timestamp = DateTime.UtcNow,
                    ErrorCount = GetTotalErrorCount(),
                    WarningCount = GetTotalWarningCount(),
                    ComponentStatus = GetComponentStatus()
                };
            }
        }

        public List<ErrorEvent> GetRecentErrors(string? component = null, int count = 100)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            lock (_lockObject)
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
            }
        }

        private int GetTotalErrorCount()
        {
            lock (_lockObject)
            {
                return _errorHistory.Values
                    .SelectMany(e => e)
                    .Count(e => !e.IsWarning);
            }
        }

        private int GetTotalWarningCount()
        {
            lock (_lockObject)
            {
                return _errorHistory.Values
                    .SelectMany(e => e)
                    .Count(e => e.IsWarning);
            }
        }

        private Dictionary<string, ComponentStatus> GetComponentStatus()
        {
            lock (_lockObject)
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
        }
    }

    public interface IErrorHandlingService
    {
        void LogError(string component, Exception ex, string? context = null);
        void LogWarning(string component, string message, string? context = null);
        SystemStatus GetSystemStatus();
        List<ErrorEvent> GetRecentErrors(string? component = null, int count = 100);
    }

    public class ErrorEvent
    {
        public DateTime Timestamp { get; set; }
        public required string Component { get; set; }
        public required string ErrorMessage { get; set; }
        public string StackTrace { get; set; } = string.Empty;
        public string? Context { get; set; }
        public bool IsWarning { get; set; }
    }

    public class SystemStatus
    {
        public DateTime Timestamp { get; set; }
        public float CpuUsage { get; set; }
        public float AvailableMemory { get; set; }
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