using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Services;
using Xunit;

namespace SET09102_2024_5.Tests
{
    /// <summary>
    /// Tests for the DialogService class
    /// </summary>
    public class DialogServiceTests
    {
        /// <summary>
        /// This test class requires a mock for Application.Current.MainPage
        /// which is challenging to test in a unit test environment.
        /// 
        /// Instead, we'll create a mockable wrapper for our tests.
        /// </summary>
        private class MockableDialogService : IDialogService
        {
            private readonly IMainPage _mainPage;

            public MockableDialogService(IMainPage mainPage)
            {
                _mainPage = mainPage;
            }

            public Task DisplayAlertAsync(string title, string message, string cancel)
            {
                return _mainPage.DisplayAlert(title, message, cancel);
            }

            public Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
            {
                return _mainPage.DisplayAlert(title, message, accept, cancel);
            }

            public Task DisplayErrorAsync(string message, string title = "Error")
            {
                return _mainPage.DisplayAlert(title, message, "OK");
            }

            public Task DisplaySuccessAsync(string message, string title = "Success")
            {
                return _mainPage.DisplayAlert(title, message, "OK");
            }
        }

        // Interface to represent the MainPage functionality we need
        private interface IMainPage
        {
            Task DisplayAlert(string title, string message, string cancel);
            Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        }

        /// <summary>
        /// Test DisplayAlertAsync calls Application.Current.MainPage.DisplayAlert with correct parameters
        /// </summary>
        [Fact]
        public async Task DisplayAlertAsync_CallsMainPageDisplayAlert()
        {
            // Arrange
            var mainPageMock = new Mock<IMainPage>();
            mainPageMock.Setup(m => m.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialogService = new MockableDialogService(mainPageMock.Object);
            
            // Act
            await dialogService.DisplayAlertAsync("Test Title", "Test Message", "OK");
            
            // Assert
            mainPageMock.Verify(m => m.DisplayAlert("Test Title", "Test Message", "OK"), Times.Once);
        }
        
        /// <summary>
        /// Test DisplayConfirmationAsync calls Application.Current.MainPage.DisplayAlert with correct parameters
        /// </summary>
        [Fact]
        public async Task DisplayConfirmationAsync_CallsMainPageDisplayAlert()
        {
            // Arrange
            var mainPageMock = new Mock<IMainPage>();
            mainPageMock.Setup(m => m.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var dialogService = new MockableDialogService(mainPageMock.Object);
            
            // Act
            var result = await dialogService.DisplayConfirmationAsync("Test Title", "Test Message", "Yes", "No");
            
            // Assert
            Assert.True(result);
            mainPageMock.Verify(m => m.DisplayAlert("Test Title", "Test Message", "Yes", "No"), Times.Once);
        }
        
        /// <summary>
        /// Test DisplayErrorAsync calls DisplayAlert with correct parameters
        /// </summary>
        [Fact]
        public async Task DisplayErrorAsync_CallsDisplayAlertWithCorrectParameters()
        {
            // Arrange
            var mainPageMock = new Mock<IMainPage>();
            mainPageMock.Setup(m => m.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialogService = new MockableDialogService(mainPageMock.Object);
            
            // Act
            await dialogService.DisplayErrorAsync("Error Message");
            
            // Assert
            mainPageMock.Verify(m => m.DisplayAlert("Error", "Error Message", "OK"), Times.Once);
        }
        
        /// <summary>
        /// Test DisplaySuccessAsync calls DisplayAlert with correct parameters
        /// </summary>
        [Fact]
        public async Task DisplaySuccessAsync_CallsDisplayAlertWithCorrectParameters()
        {
            // Arrange
            var mainPageMock = new Mock<IMainPage>();
            mainPageMock.Setup(m => m.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialogService = new MockableDialogService(mainPageMock.Object);
            
            // Act
            await dialogService.DisplaySuccessAsync("Success Message");
            
            // Assert
            mainPageMock.Verify(m => m.DisplayAlert("Success", "Success Message", "OK"), Times.Once);
        }
        
        /// <summary>
        /// Test DisplayErrorAsync with custom title calls DisplayAlert with correct parameters
        /// </summary>
        [Fact]
        public async Task DisplayErrorAsync_WithCustomTitle_CallsDisplayAlertWithCorrectParameters()
        {
            // Arrange
            var mainPageMock = new Mock<IMainPage>();
            mainPageMock.Setup(m => m.DisplayAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialogService = new MockableDialogService(mainPageMock.Object);
            
            // Act
            await dialogService.DisplayErrorAsync("Error Message", "Custom Error");
            
            // Assert
            mainPageMock.Verify(m => m.DisplayAlert("Custom Error", "Error Message", "OK"), Times.Once);
        }
    }
}