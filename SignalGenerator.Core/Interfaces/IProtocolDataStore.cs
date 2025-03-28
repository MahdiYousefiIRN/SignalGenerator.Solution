using SignalGenerator.Core.Models;

namespace SignalGenerator.Core.Interfaces
{
    public interface IProtocolDataStore
    {
        Task StoreAsync(SignalData data);
        Task<bool> DeleteAsync(DateTime timestamp);
        Task<List<SignalData>> FilterAsync(Func<SignalData, bool> predicate);
        Task<List<SignalData>> GetAllAsync();
    }
}
