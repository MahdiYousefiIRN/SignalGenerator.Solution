
using SignalGenerator.Data.Data;
using SignalGenerator.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Services
{
    public class ProtocolDataStore : IProtocolDataStore
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProtocolDataStore> _logger;

        public ProtocolDataStore(AppDbContext context, ILogger<ProtocolDataStore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendSignalAsync(SignalData signal, string protocolType)
        {
            try
            {
                signal.ProtocolType = protocolType;
                await _context.Signals.AddAsync(signal);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending signal: {ex.Message}");
                return false;
            }
        }

        public async Task<SignalData?> GetSignalAsync(string id)
        {
            try
            {
                return await _context.Signals.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving signal: {ex.Message}");
                return null;
            }
        }

        public async Task<List<SignalData>> GetSignalsAsync(string userId, DateTime? startTime = null, DateTime? endTime = null, string? protocolType = null)
        {
            try
            {
                var query = _context.Signals.AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(s => s.Id == userId);

                if (startTime.HasValue)
                    query = query.Where(s => s.Timestamp >= startTime.Value);

                if (endTime.HasValue)
                    query = query.Where(s => s.Timestamp <= endTime.Value);

                if (!string.IsNullOrEmpty(protocolType))
                    query = query.Where(s => s.ProtocolType == protocolType);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving signals: {ex.Message}");
                return new List<SignalData>();
            }
        }

        public async Task<bool> DeleteSignalAsync(string id)
        {
            try
            {
                var signal = await _context.Signals.FindAsync(id);
                if (signal == null) return false;

                _context.Signals.Remove(signal);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting signal: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSignalAsync(SignalData signal)
        {
            try
            {
                _context.Signals.Update(signal);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating signal: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveSignalsAsync(List<SignalData> signals)
        {
            try
            {
                await _context.Signals.AddRangeAsync(signals);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving signals: {ex.Message}");
                return false;
            }
        }
    }
}
