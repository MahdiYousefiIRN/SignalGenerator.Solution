using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using SignalGenerator.Helpers;

namespace SignalGenerator.Protocols.Http
{
    public class Http_Protocol : IProtocolCommunication, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILoggerService _logger;
        private const int MaxRetries = 3;
        private const int TimeoutSeconds = 30;

        // Constructor
        public Http_Protocol(IHttpClientFactory httpClientFactory, string baseUrl, ILoggerService logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _baseUrl = baseUrl;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            // Configuring retry policy with exponential backoff
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(MaxRetries, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    async (exception, timeSpan, retryCount, context) =>
                    {
                        var errorMessage = $"Retry {retryCount} after {timeSpan.TotalSeconds}s due to {exception.Exception?.Message ?? "Unknown error"}";
                        await LogAsync(errorMessage, LogLevel.Warning);
                    });
        }

        // Generic logging method
        private async Task LogAsync(string message, LogLevel logLevel = LogLevel.Info, Exception? exception = null)
        {
            await _logger.LogAsync(message, logLevel, exception);
        }

        // Receive signals from the server
        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            try
            {
                var url = $"{_baseUrl}/api/signals/get?count={config.SignalCount}";
                await LogAsync($"Requesting signals from {url}...", LogLevel.Info);

                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode();
                var signals = await response.Content.ReadFromJsonAsync<List<SignalData>>();

                if (signals == null || !signals.Any())
                {
                    await LogAsync($"⚠ No signals received from {url}", LogLevel.Warning);
                    return new List<SignalData>();
                }

                await LogAsync($"✅ Successfully received {signals.Count} signals", LogLevel.Info);
                return signals;
            }
            catch (Exception ex)
            {
                await LogAsync("Error receiving signals", LogLevel.Error, ex);
                throw new ProtocolException("Failed to receive signals", ex);
            }
        }

        // Send signals to the server
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || !signalData.Any())
            {
                await LogAsync("⚠ Attempted to send empty signal data", LogLevel.Warning);
                return false;
            }

            try
            {
                var url = $"{_baseUrl}/api/signals/post";
                await LogAsync($"🔄 Sending signals to: {url}", LogLevel.Info);

                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.PostAsJsonAsync(url, signalData));

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await LogAsync($"❌ Failed to send signals. Status Code: {response.StatusCode}, Error: {errorMessage}", LogLevel.Error);
                }
                else
                {
                    await LogAsync($"✅ Successfully sent {signalData.Count} signals", LogLevel.Info);
                }

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                await LogAsync($"🚨 HttpRequestException: {ex.Message}", LogLevel.Error, ex);
                throw new ProtocolException("Failed to send signals", ex);
            }
            catch (Exception ex)
            {
                await LogAsync($"❌ Error: {ex.Message}", LogLevel.Error, ex);
                throw new ProtocolException("Failed to send signals", ex);
            }
        }

        // Monitor server status
        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/signals/status";
                await LogAsync($"Checking status at {url}...", LogLevel.Info);

                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode();
                var status = await response.Content.ReadFromJsonAsync<bool>();

                await LogAsync($"Status check result: {status}", LogLevel.Info);
                return status;
            }
            catch (Exception ex)
            {
                await LogAsync("Error checking status", LogLevel.Error, ex);
                throw new ProtocolException("Failed to monitor status", ex);
            }
        }

        // Dispose HttpClient
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Custom exception class
    public class ProtocolException : Exception
    {
        public ProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
