using Xunit;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Services;
using SignalGenerator.Data.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Tests
{
    public class SignalDataProcessingTests
    {
        private readonly Mock<ISignalProcessorService> _signalProcessorMock;
        private readonly Mock<ILogger<SystemEvaluationService>> _loggerMock;
        private readonly SystemEvaluationService _systemEvaluationService;

        public SignalDataProcessingTests()
        {
            _signalProcessorMock = new Mock<ISignalProcessorService>();
            _loggerMock = new Mock<ILogger<SystemEvaluationService>>();
            _systemEvaluationService = new SystemEvaluationService(_signalProcessorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessSignalData_ValidData_ShouldSucceed()
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

            _signalProcessorMock.Setup(x => x.ProcessSignalAsync(It.IsAny<SignalData>()))
                .ReturnsAsync(new SignalData { /* processed data */ });

            // Act
            var result = await _systemEvaluationService.EvaluateSystemAsync(signalData);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.PerformanceMetrics);
        }

        [Fact]
        public async Task ProcessSignalData_EmptyData_ShouldFail()
        {
            // Arrange
            var signalData = new SignalData
            {
                Timestamp = DateTime.UtcNow,
                Values = new List<double>(),
                Metadata = new Dictionary<string, string>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _systemEvaluationService.EvaluateSystemAsync(signalData));
        }

        [Fact]
        public async Task ProcessSignalData_NullData_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _systemEvaluationService.EvaluateSystemAsync(null));
        }

        [Fact]
        public async Task ProcessSignalData_VerifyDataTransformation()
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

            _signalProcessorMock.Setup(x => x.ProcessSignalAsync(It.IsAny<SignalData>()))
                .ReturnsAsync(signalData);

            // Act
            var result = await _systemEvaluationService.EvaluateSystemAsync(signalData);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.TransformedData);
            Assert.Equal(signalData.Values.Count, result.TransformedData.Values.Count);
        }

        [Fact]
        public async Task ProcessSignalData_VerifyPerformanceMetrics()
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

            _signalProcessorMock.Setup(x => x.ProcessSignalAsync(It.IsAny<SignalData>()))
                .ReturnsAsync(signalData);

            // Act
            var result = await _systemEvaluationService.EvaluateSystemAsync(signalData);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PerformanceMetrics);
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "DataProcessing");
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "Transformation");
        }
    }
} 