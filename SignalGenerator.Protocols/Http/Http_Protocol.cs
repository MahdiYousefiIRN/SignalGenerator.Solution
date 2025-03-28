using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace SignalGenerator.Protocols.Http
{
    public class Http_Protocol : IProtocolCommunication
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public Http_Protocol(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
        }

        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config)
        {
            var url = $"{_baseUrl}/api/signals/get?count={config.SignalCount}";
            var signals = await _httpClient.GetFromJsonAsync<List<SignalData>>(url);
            return signals ?? new List<SignalData>();
        }

        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            var url = $"{_baseUrl}/api/signals/post";
            var response = await _httpClient.PostAsJsonAsync(url, signalData);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MonitorStatusAsync()
        {
            var url = $"{_baseUrl}/api/signals/status";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }
    }
}
