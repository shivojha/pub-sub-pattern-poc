using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace PubSubPattern.Tests
{
    [TestClass]
    public class OrderPlacedEventHandlerTests
    {
        private Mock<ILogger<OrderPlacedEventHandler>> _loggerMock;
        private OrderPlacedEventHandler _handler;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<OrderPlacedEventHandler>>();
            _handler = new OrderPlacedEventHandler(_loggerMock.Object);
        }

        [TestMethod]
        public async Task HandleAsync_WithValidEvent_ProcessesSuccessfully()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);

            // Act
            await _handler.HandleAsync(orderEvent);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing order placed event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully processed order placed event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task HandleAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            OrderPlacedEvent nullEvent = null;

            // Act
            await _handler.HandleAsync(nullEvent);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task HandleAsync_WhenExceptionOccurs_LogsErrorAndRethrows()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Throws(new Exception("Test exception"));

            // Act
            await _handler.HandleAsync(orderEvent);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing order placed event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
} 