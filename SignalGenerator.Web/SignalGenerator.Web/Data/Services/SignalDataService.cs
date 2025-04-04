namespace SignalGenerator.Web.Data.Services
{
    using SignalGenerator.Data.Interfaces;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using SignalGenerator.Web.Data.Interface;
    using SignalGenerator.Data.Models;

    public class SignalDataService : ISignalDataService
    {
        private readonly List<SignalData> _signals;
        private readonly ILogger<SignalDataService> _logger;

        public SignalDataService(ILogger<SignalDataService> logger)
        {
            _logger = logger;
            _signals = new List<SignalData>(); // شبیه‌سازی دیتابیس
        }

        public Task<int> GetTotalSignalsAsync()
        {
            return Task.FromResult(_signals.Count);
        }

        public Task<List<string>> GetActiveProtocolsAsync()
        {
            var protocols = _signals.Select(s => s.ProtocolType).Distinct().ToList();
            return Task.FromResult(protocols);
        }

        public Task<List<SignalData>> GetLatestSignalsAsync(int count)
        {
            var latest = _signals.OrderByDescending(s => s.Timestamp).Take(count).ToList();
            return Task.FromResult(latest);
        }
    }

}
