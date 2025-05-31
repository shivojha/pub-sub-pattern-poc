using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace PubSubPattern.Tests
{
    [TestClass]
    public class EventAggregatorTests
    {
        private Mock<ILogger<EventAggregator>> _loggerMock;
        private IEventAggregator _eventAggregator;
        private Mock<IEventStore> _eventStoreMock;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<EventAggregator>>();
            _eventStoreMock = new Mock<IEventStore>();
            _eventAggregator = new EventAggregator(_loggerMock.Object, _eventStoreMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            Action<OrderPlacedEvent> nullAction = null;

            // Act
            _eventAggregator.Subscribe(nullAction);
        }

        [TestMethod]
        public void Subscribe_WithValidAction_SubscribesSuccessfully()
        {
            // Arrange
            var actionCalled = false;
            Action<OrderPlacedEvent> action = _ => actionCalled = true;

            // Act
            _eventAggregator.Subscribe(action);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Subscribed to event type")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            OrderPlacedEvent nullEvent = null;

            // Act
            await _eventAggregator.PublishAsync(nullEvent);
        }

        [TestMethod]
        public async Task PublishAsync_WithNoSubscribers_LogsWarning()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);

            // Act
            await _eventAggregator.PublishAsync(orderEvent);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No subscribers found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task PublishAsync_WithSubscriber_CallsSubscriber()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);
            var actionCalled = false;
            Action<OrderPlacedEvent> action = _ => actionCalled = true;

            _eventAggregator.Subscribe(action);

            // Act
            await _eventAggregator.PublishAsync(orderEvent);

            // Assert
            Assert.IsTrue(actionCalled);
        }

        [TestMethod]
        public async Task PublishAsync_WithMultipleSubscribers_CallsAllSubscribers()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);
            var action1Called = false;
            var action2Called = false;

            _eventAggregator.Subscribe<OrderPlacedEvent>(_ => action1Called = true);
            _eventAggregator.Subscribe<OrderPlacedEvent>(_ => action2Called = true);

            // Act
            await _eventAggregator.PublishAsync(orderEvent);

            // Assert
            Assert.IsTrue(action1Called);
            Assert.IsTrue(action2Called);
        }

        [TestMethod]
        public async Task PublishAsync_WithFailingSubscriber_LogsErrorAndContinues()
        {
            // Arrange
            var orderEvent = new OrderPlacedEvent("123", "C001", DateTime.Now);
            var action2Called = false;

            _eventAggregator.Subscribe<OrderPlacedEvent>(_ => throw new Exception("Test exception"));
            _eventAggregator.Subscribe<OrderPlacedEvent>(_ => action2Called = true);

            // Act
            await _eventAggregator.PublishAsync(orderEvent);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error publishing event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            Assert.IsTrue(action2Called);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Unsubscribe_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange
            Action<OrderPlacedEvent> nullAction = null;

            // Act
            _eventAggregator.Unsubscribe(nullAction);
        }

        [TestMethod]
        public async Task Unsubscribe_WithValidAction_UnsubscribesSuccessfully()
        {
            // Arrange
            var actionCalled = false;
            Action<OrderPlacedEvent> action = _ => actionCalled = true;

            _eventAggregator.Subscribe(action);
            _eventAggregator.Unsubscribe(action);

            // Act
            await _eventAggregator.PublishAsync(new OrderPlacedEvent("123", "C001", DateTime.Now));

            // Assert
            Assert.IsFalse(actionCalled);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unsubscribed delegate from event type")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
} 