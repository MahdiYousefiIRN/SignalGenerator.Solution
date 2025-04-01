namespace SignalGenerator.Web.Data.Interface
{
    using SignalGenerator.Core.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISignalDataService
    {
        Task<int> GetTotalSignalsAsync();
        Task<List<string>> GetActiveProtocolsAsync();
        Task<List<SignalData>> GetLatestSignalsAsync(int count);
    }

}
