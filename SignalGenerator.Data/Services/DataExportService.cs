using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using CsvHelper;
using System.Globalization;

namespace SignalGenerator.Data.Services
{
    public class DataExportService : IDataExportService
    {
        private readonly IProtocolDataStore _dataStore;
        private readonly ILogger<DataExportService> _logger;

        public DataExportService(IProtocolDataStore dataStore, ILogger<DataExportService> logger)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> ExportToCsvAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(signals);
                await writer.FlushAsync();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportToJsonAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);
                var json = System.Text.Json.JsonSerializer.Serialize(signals, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to JSON");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Signals");

                worksheet.Cell(1, 1).Value = "Timestamp";
                worksheet.Cell(1, 2).Value = "Frequency (Hz)";
                worksheet.Cell(1, 3).Value = "Power (dB)";
                worksheet.Cell(1, 4).Value = "Protocol Type";
                worksheet.Cell(1, 5).Value = "Coil Status";
                worksheet.Cell(1, 6).Value = "Discrete Input Status";

                for (int i = 0; i < signals.Count; i++)
                {
                    var signal = signals[i];
                    worksheet.Cell(i + 2, 1).Value = signal.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    worksheet.Cell(i + 2, 2).Value = signal.Frequency;
                    worksheet.Cell(i + 2, 3).Value = signal.Power;
                    worksheet.Cell(i + 2, 4).Value = signal.ProtocolType;
                    worksheet.Cell(i + 2, 5).Value = signal.CoilStatus;
                    worksheet.Cell(i + 2, 6).Value = signal.DiscreteInputStatus;
                }

                worksheet.Columns().AdjustToContents();
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to Excel");
                throw;
            }
        }
    }

    public interface IDataExportService
    {
        Task<byte[]> ExportToCsvAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
        Task<byte[]> ExportToJsonAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
        Task<byte[]> ExportToExcelAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
    }
}
