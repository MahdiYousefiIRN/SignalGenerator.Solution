using Xunit;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Services;
using SignalGenerator.Data.Services;
using Moq;
using System;
using System.Threading.Tasks;

namespace SignalGenerator.Tests
{
    public class ErrorHandlingTests
    {
        private readonly Mock<IErrorHandlingService> _errorHandlingMock;
        private readonly Mock<ILogger<ErrorHandlingService>> _loggerMock;
        private readonly ErrorHandlingService _errorHandlingService;

        public ErrorHandlingTests()
        {
            _errorHandlingMock = new Mock<IErrorHandlingService>();
            _loggerMock = new Mock<ILogger<ErrorHandlingService>>();
            _errorHandlingService = new ErrorHandlingService(_loggerMock.Object);
        }

        [Fact]
        public async Task HandleError_ValidError_ShouldLogAndReturnErrorResponse()
        {
            // Arrange
            var error = new Exception("Test error");
            var context = "Test context";

            // Act
            var result = await _errorHandlingService.HandleErrorAsync(error, context);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(context, result.Context);
        }

        [Fact]
        public async Task HandleError_NullError_ShouldThrowException()
        {
            // Arrange
            Exception error = null;
            var context = "Test context";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _errorHandlingService.HandleErrorAsync(error, context));
        }

        [Fact]
        public async Task HandleError_NullContext_ShouldThrowException()
        {
            // Arrange
            var error = new Exception("Test error");
            string context = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _errorHandlingService.HandleErrorAsync(error, context));
        }

        [Fact]
        public async Task HandleError_VerifyErrorDetails()
        {
            // Arrange
            var error = new Exception("Test error");
            var context = "Test context";

            // Act
            var result = await _errorHandlingService.HandleErrorAsync(error, context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(error.Message, result.ErrorMessage);
            Assert.Equal(error.GetType().Name, result.ErrorType);
            Assert.NotNull(result.Timestamp);
        }

        [Fact]
        public async Task HandleError_VerifyLogging()
        {
            // Arrange
            var error = new Exception("Test error");
            var context = "Test context";

            // Act
            await _errorHandlingService.HandleErrorAsync(error, context);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
} 