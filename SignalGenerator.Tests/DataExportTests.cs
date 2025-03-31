using Xunit;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Services;
using SignalGenerator.Data.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SignalGenerator.Core.Interfaces;

namespace SignalGenerator.Tests
{
    public class DataExportTests
    {
        private readonly Mock<IDataExportService> _dataExportMock;
        private readonly Mock<ILogger<DataExportService>> _loggerMock;
        private readonly DataExportService _dataExportService;

        public DataExportTests()
        {
            _dataExportMock = new Mock<IDataExportService>();
            _loggerMock = new Mock<ILogger<DataExportService>>();
            var dataStoreMock = new Mock<SignalGenerator.Data.Interfaces.IProtocolDataStore>(); // Corrected namespace
            _dataExportService = new DataExportService(dataStoreMock.Object, _loggerMock.Object); // Pass the mock object
        }

        [Fact]
        public async Task ExportData_ValidData_ShouldSucceed()
        {
            // Arrange
            var signalData = new SignalData
            {
                Timestamp = DateTime.UtcNow,
                Values = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 },
                Metadata = new Dictionary<string, string>
                    {
                        { "Frequency", "1000" },
                        { "Amplitude", "1.0" }
                    }
            };

            var exportConfig = new ExportConfig
            {
                Format = "CSV",
                IncludeMetadata = true,
                IncludeTimestamp = true
            };

            // Act
            var result = await _dataExportService.ExportDataAsync(signalData, exportConfig);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.ExportPath);
        }

        [Fact]
        public async Task ExportData_InvalidFormat_ShouldFail()
        {
            // Arrange
            var signalData = new SignalData
            {
                Timestamp = DateTime.UtcNow,
                Values = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 }
            };

            var exportConfig = new ExportConfig
            {
                Format = "InvalidFormat",
                IncludeMetadata = true,
                IncludeTimestamp = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _dataExportService.ExportDataAsync(signalData, exportConfig));
        }

        [Fact]
        public async Task ExportData_NullData_ShouldThrowException()
        {
            // Arrange
            SignalData signalData = null;
            var exportConfig = new ExportConfig
            {
                Format = "CSV",
                IncludeMetadata = true,
                IncludeTimestamp = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _dataExportService.ExportDataAsync(signalData, exportConfig));
        }

        [Fact]
        public async Task ExportData_VerifyExportContent()
        {
            // Arrange
            var signalData = new SignalData
            {
                Timestamp = DateTime.UtcNow,
                Values = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 },
                Metadata = new Dictionary<string, string>
                    {
                        { "Frequency", "1000" },
                        { "Amplitude", "1.0" }
                    }
            };

            var exportConfig = new ExportConfig
            {
                Format = "CSV",
                IncludeMetadata = true,
                IncludeTimestamp = true
            };

            // Act
            var result = await _dataExportService.ExportDataAsync(signalData, exportConfig);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.ExportPath);
            Assert.True(System.IO.File.Exists(result.ExportPath));
        }

        [Fact]
        public async Task ExportData_VerifyExportPerformance()
        {
            // Arrange
            var signalData = new SignalData
            {
                Timestamp = DateTime.UtcNow,
                Values = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 },
                Metadata = new Dictionary<string, string>
                    {
                        { "Frequency", "1000" },
                        { "Amplitude", "1.0" }
                    }
            };

            var exportConfig = new ExportConfig
            {
                Format = "CSV",
                IncludeMetadata = true,
                IncludeTimestamp = true
            };

            // Act
            var result = await _dataExportService.ExportDataAsync(signalData, exportConfig);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PerformanceMetrics);
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "DataExport");
            Assert.True(result.PerformanceMetrics[0].TotalDuration < 1000); // Should complete within 1 second
        }
    }
} 