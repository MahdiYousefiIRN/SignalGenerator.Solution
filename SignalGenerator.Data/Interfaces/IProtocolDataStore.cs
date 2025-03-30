using SignalGenerator.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Interfaces
{
    public interface IProtocolDataStore
    {
        Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime, DateTime? endTime, string? protocolType = null);
        Task<bool> SaveSignalsAsync(List<SignalData> signals);
        Task<bool> SendSignalAsync(SignalData signal, string protocol);
        Task<bool> DeleteSignalAsync(string id);
        Task<bool> UpdateSignalAsync(SignalData signal);
        Task<SignalData?> GetSignalAsync(string id); // متد جدید برای دریافت یک سیگنال خاص
    }
}
