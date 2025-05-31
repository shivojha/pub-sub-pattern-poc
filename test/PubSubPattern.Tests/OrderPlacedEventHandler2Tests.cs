using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace PubSubPattern.Tests
{
    [TestClass]
    public class OrderPlacedEventHandler2Tests
    {
        private Mock<ILogger<OrderPlacedEventHandler2>> _loggerMock;
        private OrderPlacedEventHandler2 _handler;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<OrderPlacedEventHandler2>>();
            _handler = new OrderPlacedEventHandler2(_loggerMock.Object);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing OrderPlacedEvent event (Version 1)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully processed OrderPlacedEvent event (Version 1)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task HandleAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            BaseEvent nullEvent = null;

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
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing event of type OrderPlacedEvent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

         [TestMethod]
        public async Task HandleAsync_WithValidV2Event_ProcessesSuccessfully()
        {
            // Arrange
            var orderEvent = new OrderPlacedEventV2("123", "C001", DateTime.Now, "USA Address");

            // Act
            await _handler.HandleAsync(orderEvent);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing OrderPlacedEventV2 event (Version 2)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully processed OrderPlacedEventV2 event (Version 2)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
} 