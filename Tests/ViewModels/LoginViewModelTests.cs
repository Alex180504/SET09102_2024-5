using System;
using System.Threading.Tasks;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.ViewModels;
using Xunit;

namespace SET09102_2024_5.Tests.ViewModels
{
    /// <summary>
    /// Tests for the LoginViewModel class
    /// </summary>
    public class LoginViewModelTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<INavigationService> _mockNavigationService;

        public LoginViewModelTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockNavigationService = new Mock<INavigationService>();
        }

        /// <summary>
        /// Test that LoginCommand cannot execute when email is empty
        /// </summary>
        [Fact]
        public void LoginCommand_CannotExecute_WhenEmailIsEmpty()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "";
            viewModel.Password = "password123";

            // Act
            bool canExecute = viewModel.LoginCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        /// <summary>
        /// Test that LoginCommand cannot execute when password is empty
        /// </summary>
        [Fact]
        public void LoginCommand_CannotExecute_WhenPasswordIsEmpty()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "user@example.com";
            viewModel.Password = "";

            // Act
            bool canExecute = viewModel.LoginCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        /// <summary>
        /// Test that LoginCommand can execute when both email and password are provided
        /// </summary>
        [Fact]
        public void LoginCommand_CanExecute_WhenBothEmailAndPasswordProvided()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";

            // Act
            bool canExecute = viewModel.LoginCommand.CanExecute(null);

            // Assert
            Assert.True(canExecute);
        }

        /// <summary>
        /// Test that LoginAsync with valid credentials navigates to main page
        /// </summary>
        [Fact]
        public async Task LoginCommand_WithValidCredentials_NavigatesToMainPage()
        {
            // Arrange
            var user = new User { Email = "user@example.com", UserId = 1 };
            
            _mockAuthService.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(user);
            
            _mockNavigationService.Setup(n => n.NavigateToMainPageAsync())
                .Returns(Task.CompletedTask);

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _mockAuthService.Verify(s => s.AuthenticateAsync("user@example.com", "password123"), Times.Once);
            _mockAuthService.Verify(s => s.SetCurrentUser(user), Times.Once);
            _mockNavigationService.Verify(n => n.NavigateToMainPageAsync(), Times.Once);
            Assert.Empty(viewModel.Email);
            Assert.Empty(viewModel.Password);
        }

        /// <summary>
        /// Test that LoginAsync with invalid credentials shows error message
        /// </summary>
        [Fact]
        public async Task LoginCommand_WithInvalidCredentials_ShowsErrorMessage()
        {
            // Arrange
            _mockAuthService.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((User)null);

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "wrong@example.com";
            viewModel.Password = "wrongpassword";

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            _mockAuthService.Verify(s => s.AuthenticateAsync("wrong@example.com", "wrongpassword"), Times.Once);
            _mockNavigationService.Verify(n => n.NavigateToMainPageAsync(), Times.Never);
            Assert.Equal("Invalid email or password.", viewModel.ErrorMessage);
            Assert.Equal("wrong@example.com", viewModel.Email);
            Assert.Equal("wrongpassword", viewModel.Password);
        }

        /// <summary>
        /// Test that RegisterCommand navigates to registration page
        /// </summary>
        [Fact]
        public async Task RegisterCommand_NavigatesToRegisterPage()
        {
            // Arrange
            _mockNavigationService.Setup(n => n.NavigateToRegisterAsync())
                .Returns(Task.CompletedTask);

            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);

            // Act
            await viewModel.RegisterCommand.ExecuteAsync(null);

            // Assert
            _mockNavigationService.Verify(n => n.NavigateToRegisterAsync(), Times.Once);
        }
        
        /// <summary>
        /// Test that setting Email updates LoginCommand's CanExecute
        /// </summary>
        [Fact]
        public void ChangingEmail_UpdatesLoginCommandCanExecute()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "";
            viewModel.Password = "password123";
            bool initialCanExecute = viewModel.LoginCommand.CanExecute(null);
            
            // Act
            viewModel.Email = "user@example.com";
            bool finalCanExecute = viewModel.LoginCommand.CanExecute(null);
            
            // Assert
            Assert.False(initialCanExecute);
            Assert.True(finalCanExecute);
        }
        
        /// <summary>
        /// Test that setting Password updates LoginCommand's CanExecute
        /// </summary>
        [Fact]
        public void ChangingPassword_UpdatesLoginCommandCanExecute()
        {
            // Arrange
            var viewModel = new LoginViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.Email = "user@example.com";
            viewModel.Password = "";
            bool initialCanExecute = viewModel.LoginCommand.CanExecute(null);
            
            // Act
            viewModel.Password = "password123";
            bool finalCanExecute = viewModel.LoginCommand.CanExecute(null);
            
            // Assert
            Assert.False(initialCanExecute);
            Assert.True(finalCanExecute);
        }
    }
}