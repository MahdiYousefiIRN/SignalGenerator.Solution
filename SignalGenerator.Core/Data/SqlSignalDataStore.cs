
    using global::SignalGenerator.Core.Models;
    using Microsoft.EntityFrameworkCore;
    using SignalGenerator.Core.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace SignalGenerator.Core.Data
    {
        public class SqlSignalDataStore : IProtocolDataStore
        {
            private readonly AppDbContext _dbContext;

            public SqlSignalDataStore(AppDbContext dbContext)
            {
                _dbContext = dbContext;
            }

            public async Task StoreAsync(SignalData data)
            {
                await _dbContext.Signals.AddAsync(data);
                await _dbContext.SaveChangesAsync();
            }

            public async Task<bool> DeleteAsync(DateTime timestamp)
            {
                var signal = await _dbContext.Signals
                    .FirstOrDefaultAsync(x => x.Timestamp == timestamp);
                if (signal != null)
                {
                    _dbContext.Signals.Remove(signal);
                    await _dbContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }

            public async Task<List<SignalData>> FilterAsync(Func<SignalData, bool> predicate)
            {
                return await Task.FromResult(_dbContext.Signals.Where(predicate).ToList());
            }

            public async Task<List<SignalData>> GetAllAsync()
            {
                return await _dbContext.Signals.ToListAsync();
            }
        }
    }


