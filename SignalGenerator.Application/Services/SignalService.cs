using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Application.Services
{
    public class SignalService : ISignalService
    {
        private readonly IProtocolDataStore _dataStore;

        public SignalService(IProtocolDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime, DateTime? endTime, string? protocolType = null)
        {
            return await _dataStore.GetSignalsAsync(userId, startTime, endTime, protocolType);
        }

        public async Task<bool> AddSignalAsync(SignalData signal)
        {
            return await _dataStore.SaveSignalsAsync(new List<SignalData> { signal });
        }
    }

    public interface ISignalService
    {
        Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime, DateTime? endTime, string? protocolType = null);
        Task<bool> AddSignalAsync(SignalData signal);
    }
} 