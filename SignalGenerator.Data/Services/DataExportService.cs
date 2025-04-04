using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using CsvHelper;
using System.Globalization;
using ClosedXML.Excel;

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

        /// <summary>
        /// Exports signal data to a CSV file.
        /// </summary>
        public async Task<byte[]> ExportToCsvAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                _logger.LogInformation("📄 Fetching data for CSV export...");
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);

                if (!signals.Any())
                {
                    _logger.LogWarning("⚠ No data available for CSV export.");
                    return Array.Empty<byte>();
                }

                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(signals);
                await writer.FlushAsync();

                _logger.LogInformation("✅ CSV export completed successfully.");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⛔ Error exporting data to CSV.");
                throw new Exception("CSV export failed. Please check logs for details.", ex);
            }
        }

        /// <summary>
        /// Exports signal data to a JSON file.
        /// </summary>
        public async Task<byte[]> ExportToJsonAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                _logger.LogInformation("📝 Fetching data for JSON export...");
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);

                if (!signals.Any())
                {
                    _logger.LogWarning("⚠ No data available for JSON export.");
                    return Array.Empty<byte>();
                }

                var json = System.Text.Json.JsonSerializer.Serialize(signals, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                _logger.LogInformation("✅ JSON export completed successfully.");
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⛔ Error exporting data to JSON.");
                throw new Exception("JSON export failed. Please check logs for details.", ex);
            }
        }

        /// <summary>
        /// Exports signal data to an Excel file (.xlsx).
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(DateTime startTime, DateTime endTime, string? protocolType = null)
        {
            try
            {
                _logger.LogInformation("📊 Fetching data for Excel export...");
                var signals = await _dataStore.GetSignalsAsync("", startTime, endTime, protocolType);

                if (!signals.Any())
                {
                    _logger.LogWarning("⚠ No data available for Excel export.");
                    return Array.Empty<byte>();
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Signals");

                // Headers
                var headers = new[] { "Timestamp", "Frequency (Hz)", "Power (dB)", "Protocol Type", "Coil Status", "Discrete Input Status" };
                for (int col = 0; col < headers.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = headers[col];
                    worksheet.Cell(1, col + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Data Rows
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

                _logger.LogInformation("✅ Excel export completed successfully.");
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⛔ Error exporting data to Excel.");
                throw new Exception("Excel export failed. Please check logs for details.", ex);
            }
        }
    }

    /// <summary>
    /// Interface for exporting signal data.
    /// </summary>
    public interface IDataExportService
    {
        Task<byte[]> ExportToCsvAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
        Task<byte[]> ExportToJsonAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
        Task<byte[]> ExportToExcelAsync(DateTime startTime, DateTime endTime, string? protocolType = null);
    }
}
