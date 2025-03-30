using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Data
{
    public class SqlSignalDataStore : IProtocolDataStore
    {
        private readonly AppDbContext _dbContext;

        public SqlSignalDataStore(AppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime, DateTime? endTime, string? protocolType = null)
        {
            var query = _dbContext.Signals.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(s => s.Id == userId);
            }

            if (startTime.HasValue)
            {
                query = query.Where(s => s.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(s => s.Timestamp <= endTime.Value);
            }

            if (!string.IsNullOrEmpty(protocolType))
            {
                query = query.Where(s => s.ProtocolType == protocolType);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> SaveSignalsAsync(List<SignalData> signals)
        {
            try
            {
                await _dbContext.Signals.AddRangeAsync(signals);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendSignalAsync(SignalData signal, string protocol)
        {
            try
            {
                signal.ProtocolType = protocol;
                await _dbContext.Signals.AddAsync(signal);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSignalAsync(string id)
        {
            var signal = await _dbContext.Signals.FirstOrDefaultAsync(s => s.Id == id);
            if (signal != null)
            {
                _dbContext.Signals.Remove(signal);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateSignalAsync(SignalData signal)
        {
            try
            {
                var existingSignal = await _dbContext.Signals.FirstOrDefaultAsync(s => s.Id == signal.Id);
                if (existingSignal == null)
                {
                    return false;
                }
                _dbContext.Entry(existingSignal).CurrentValues.SetValues(signal);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<SignalData?> GetSignalAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}
