using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Protocols.Http
{
    public class Http_Protocol : IProtocolCommunication
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<Http_Protocol> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private const int MaxRetries = 3;
        private const int TimeoutSeconds = 30;

        public Http_Protocol(string baseUrl, ILogger<Http_Protocol> logger)
        {
            _baseUrl = baseUrl;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // بررسی مقدار null
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };

            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(MaxRetries, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} after {TimeSpan}s due to {ExceptionMessage}",
                            retryCount, timeSpan.TotalSeconds, exception.Exception.Message);
                    });
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            try
            {
                var url = $"{_baseUrl}/api/signals/get?count={config.SignalCount}";
                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode();
                var signals = await response.Content.ReadFromJsonAsync<List<SignalData>>();

                if (signals == null || !signals.Any())
                {
                    _logger.LogWarning("No signals received from {Url}", url);
                    return new List<SignalData>();
                }

                _logger.LogInformation("Successfully received {Count} signals", signals.Count);
                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving signals from {BaseUrl}", _baseUrl);
                throw new ProtocolException("Failed to receive signals", ex);
            }
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || !signalData.Any())
            {
                _logger.LogWarning("Attempted to send empty signal data");
                Console.WriteLine("⚠ Warning: Attempted to send empty signal data");
                return false;
            }

            try
            {
                var url = $"{_baseUrl}/api/signals/post";
                Console.WriteLine($"🔄 Sending signals to: {url}");

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.PostAsJsonAsync(url, signalData));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Failed to send signals. Status Code: {response.StatusCode}, Error: {errorMessage}");
                }
                else
                {
                    Console.WriteLine($"✅ Successfully sent {signalData.Count} signals");
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"🚨 HttpRequestException: {ex.Message}");
                throw new ProtocolException("Failed to send signals", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw new ProtocolException("Failed to send signals", ex);
            }
        }

        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/signals/status";
                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode();
                var status = await response.Content.ReadFromJsonAsync<bool>();
                _logger.LogInformation("Status check result: {Status}", status);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status at {BaseUrl}", _baseUrl);
                throw new ProtocolException("Failed to monitor status", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class ProtocolException : Exception
    {
        public ProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
