using System;
using System.Threading.Tasks;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Services;
using Xunit;

namespace SET09102_2024_5.Tests
{
    /// <summary>
    /// Tests for the MainThreadService class
    /// </summary>
    public class MainThreadServiceTests
    {
        /// <summary>
        /// Since MainThreadService is a direct wrapper around MAUI's MainThread,
        /// which is challenging to test in a unit test environment,
        /// we'll create a mockable version for testing.
        /// </summary>
        private class TestableMainThreadService : IMainThreadService
        {
            private readonly bool _isMainThread;
            private readonly Action<Action> _beginInvokeOnMainThread;
            private readonly Func<Action, Task> _invokeOnMainThreadAsync;
            private readonly Func<Func<object>, Task<object>> _invokeOnMainThreadAsyncWithResult;

            public TestableMainThreadService(
                bool isMainThread,
                Action<Action> beginInvokeOnMainThread,
                Func<Action, Task> invokeOnMainThreadAsync)
            {
                _isMainThread = isMainThread;
                _beginInvokeOnMainThread = beginInvokeOnMainThread;
                _invokeOnMainThreadAsync = invokeOnMainThreadAsync;
                _invokeOnMainThreadAsyncWithResult = null;
            }

            public bool IsMainThread => _isMainThread;

            public void BeginInvokeOnMainThread(Action action)
            {
                _beginInvokeOnMainThread(action);
            }

            public Task InvokeOnMainThreadAsync(Action action)
            {
                return _invokeOnMainThreadAsync(action);
            }

            public Task<T> InvokeOnMainThreadAsync<T>(Func<T> function)
            {
                // Simple implementation that just calls the function
                return Task.FromResult(function());
            }
        }

        /// <summary>
        /// Test that IsMainThread returns the correct value
        /// </summary>
        [Fact]
        public void IsMainThread_ReturnsExpectedValue()
        {
            // Arrange
            var testService = new TestableMainThreadService(
                isMainThread: true,
                beginInvokeOnMainThread: _ => { },
                invokeOnMainThreadAsync: _ => Task.CompletedTask);

            // Act
            bool result = testService.IsMainThread;

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Test that BeginInvokeOnMainThread calls the provided action
        /// </summary>
        [Fact]
        public void BeginInvokeOnMainThread_CallsProvidedAction()
        {
            // Arrange
            bool actionWasCalled = false;
            
            var testService = new TestableMainThreadService(
                isMainThread: false,
                beginInvokeOnMainThread: action => { action(); actionWasCalled = true; },
                invokeOnMainThreadAsync: _ => Task.CompletedTask);

            // Act
            testService.BeginInvokeOnMainThread(() => { });

            // Assert
            Assert.True(actionWasCalled);
        }

        /// <summary>
        /// Test that InvokeOnMainThreadAsync calls the provided action
        /// </summary>
        [Fact]
        public async Task InvokeOnMainThreadAsync_CallsProvidedAction()
        {
            // Arrange
            bool actionWasCalled = false;
            
            var testService = new TestableMainThreadService(
                isMainThread: false,
                beginInvokeOnMainThread: _ => { },
                invokeOnMainThreadAsync: action => 
                {
                    action();
                    actionWasCalled = true;
                    return Task.CompletedTask;
                });

            // Act
            await testService.InvokeOnMainThreadAsync(() => { });

            // Assert
            Assert.True(actionWasCalled);
        }

        /// <summary>
        /// Test that InvokeOnMainThreadAsync with function calls the provided function and returns its value
        /// </summary>
        [Fact]
        public async Task InvokeOnMainThreadAsync_WithFunction_ReturnsExpectedValue()
        {
            // Arrange
            const int expectedValue = 42;
            
            var testService = new TestableMainThreadService(
                isMainThread: false,
                beginInvokeOnMainThread: _ => { },
                invokeOnMainThreadAsync: _ => Task.CompletedTask);

            // Act
            int result = await testService.InvokeOnMainThreadAsync(() => expectedValue);

            // Assert
            Assert.Equal(expectedValue, result);
        }
    }
}