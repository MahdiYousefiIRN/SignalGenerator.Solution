using Xunit;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Services;
using SignalGenerator.Data.Services;
using Moq;
using System;
using System.Threading.Tasks;

namespace SignalGenerator.Tests
{
    public class SignalTransmissionTests
    {
        private readonly Mock<IProtocolDataStore> _protocolDataStoreMock;
        private readonly Mock<ILogger<SignalTestingService>> _loggerMock;
        private readonly SignalTestingService _signalTestingService;

        public SignalTransmissionTests()
        {
            _protocolDataStoreMock = new Mock<IProtocolDataStore>();
            _loggerMock = new Mock<ILogger<SignalTestingService>>();
            _signalTestingService = new SignalTestingService(_protocolDataStoreMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task TestSignalTransmission_ValidConfig_ShouldSucceed()
        {
            // Arrange
            var config = new SignalConfig
            {
                Protocol = "TCP",
                Frequency = 1000,
                Amplitude = 1.0,
                Duration = 1000,
                SampleRate = 44100
            };

            // Act
            var result = await _signalTestingService.TestSignalTransmissionAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.PerformanceMetrics);
            Assert.True(result.PerformanceMetrics.Count > 0);
        }

        [Fact]
        public async Task TestSignalTransmission_InvalidConfig_ShouldFail()
        {
            // Arrange
            var config = new SignalConfig
            {
                Protocol = "InvalidProtocol",
                Frequency = -1,
                Amplitude = -1,
                Duration = -1,
                SampleRate = -1
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _signalTestingService.TestSignalTransmissionAsync(config));
        }

        [Fact]
        public async Task TestSignalTransmission_NullConfig_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _signalTestingService.TestSignalTransmissionAsync(null));
        }

        [Fact]
        public async Task TestSignalTransmission_VerifySignalIntegrity()
        {
            // Arrange
            var config = new SignalConfig
            {
                Protocol = "TCP",
                Frequency = 1000,
                Amplitude = 1.0,
                Duration = 1000,
                SampleRate = 44100
            };

            // Act
            var result = await _signalTestingService.TestSignalTransmissionAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(result.SignalIntegrity > 0.95); // Signal integrity should be high
        }

        [Fact]
        public async Task TestSignalTransmission_PerformanceMetrics()
        {
            // Arrange
            var config = new SignalConfig
            {
                Protocol = "TCP",
                Frequency = 1000,
                Amplitude = 1.0,
                Duration = 1000,
                SampleRate = 44100
            };

            // Act
            var result = await _signalTestingService.TestSignalTransmissionAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PerformanceMetrics);
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "SignalGeneration");
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "Transmission");
            Assert.Contains(result.PerformanceMetrics, m => m.Operation == "Verification");
        }
    }
} 