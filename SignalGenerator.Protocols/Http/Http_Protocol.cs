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

        // Constructor to initialize the HttpClient, URL, logger, and retry policy
        public Http_Protocol(IHttpClientFactory httpClientFactory, string baseUrl, ILoggerService logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _baseUrl = baseUrl;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            // Configuring retry policy with exponential backoff in case of transient failures
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>() // Handles timeouts and request exceptions
                .WaitAndRetryAsync(MaxRetries, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 1s, 2s, 4s
                    async (exception, timeSpan, retryCount, context) =>
                    {
                        var errorMessage = $"Retry {retryCount} after {timeSpan.TotalSeconds}s due to {exception.Exception?.Message ?? "Unknown error"}";
                        await LogAsync(errorMessage, LogLevel.Warning); // Log retry information
                    });
        }

        // Generic logging method to handle logging at different levels
        private async Task LogAsync(string message, LogLevel logLevel = LogLevel.Info, Exception? exception = null)
        {
            await _logger.LogAsync(message, logLevel, exception);
        }

        // Method to receive signals from the server with the provided configuration
        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalData config)
        {
            try
            {
                var url = $"{_baseUrl}/signals/get?count={config.SignalCount}";
                await LogAsync($"Requesting signals from {url}...", LogLevel.Info);

                // Execute the request with retry policy
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode(); // Ensure the response is successful (status code 2xx)
                var signals = await response.Content.ReadFromJsonAsync<List<SignalData>>();

                // Check if signals were received
                if (signals == null || !signals.Any())
                {
                    await LogAsync($"⚠ No signals received from {url}", LogLevel.Warning);
                    return new List<SignalData>(); // Return an empty list if no signals were received
                }

                await LogAsync($"✅ Successfully received {signals.Count} signals", LogLevel.Info);
                return signals; // Return the list of signals
            }
            catch (Exception ex)
            {
                await LogAsync("Error receiving signals", LogLevel.Error, ex);
                throw new ProtocolException("Failed to receive signals", ex); // Throw custom ProtocolException
            }
        }

        // Method to send signals to the server
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            if (signalData == null || !signalData.Any())
            {
                await LogAsync("⚠ Attempted to send empty signal data", LogLevel.Warning);
                return false; // If no data to send, return false
            }

            try
            {
                var url = $"{_baseUrl}/signals/post";
                await LogAsync($"🔄 Sending signals to: {url}", LogLevel.Info);

                // Send signal data with retry policy
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.PostAsJsonAsync(url, signalData));

                // Check for success or failure in sending data
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await LogAsync($"❌ Failed to send signals. Status Code: {response.StatusCode}, Error: {errorMessage}", LogLevel.Error);
                }
                else
                {
                    await LogAsync($"✅ Successfully sent {signalData.Count} signals", LogLevel.Info);
                }

                return response.IsSuccessStatusCode; // Return true if request was successful
            }
            catch (HttpRequestException ex)
            {
                await LogAsync($"🚨 HttpRequestException: {ex.Message}", LogLevel.Error, ex);
                throw new ProtocolException("Failed to send signals", ex); // Throw ProtocolException in case of HttpRequestException
            }
            catch (Exception ex)
            {
                await LogAsync($"❌ Error: {ex.Message}", LogLevel.Error, ex);
                throw new ProtocolException("Failed to send signals", ex); // Throw ProtocolException for any other exceptions
            }
        }

        // Method to monitor the server status
        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                var url = $"{_baseUrl}/signals/status";
                await LogAsync($"Checking status at {url}...", LogLevel.Info);

                // Check server status with retry policy
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _httpClient.GetAsync(url));

                response.EnsureSuccessStatusCode(); // Ensure the response is successful
                var status = await response.Content.ReadFromJsonAsync<bool>();

                await LogAsync($"Status check result: {status}", LogLevel.Info);
                return status; // Return the status result
            }
            catch (Exception ex)
            {
                await LogAsync("Error checking status", LogLevel.Error, ex);
                throw new ProtocolException("Failed to monitor status", ex); // Throw ProtocolException if any exception occurs
            }
        }

        // Dispose HttpClient to release resources
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Custom exception class for protocol-specific exceptions
    public class ProtocolException : Exception
    {
        public ProtocolException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
