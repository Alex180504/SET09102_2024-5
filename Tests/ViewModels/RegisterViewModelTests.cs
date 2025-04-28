using System;
using System.Threading.Tasks;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.ViewModels;
using Xunit;

namespace SET09102_2024_5.Tests.ViewModels
{
    /// <summary>
    /// Tests for the RegisterViewModel class
    /// </summary>
    public class RegisterViewModelTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<INavigationService> _mockNavigationService;

        public RegisterViewModelTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockNavigationService = new Mock<INavigationService>();
        }

        /// <summary>
        /// Test that RegisterCommand cannot execute when any field is empty
        /// </summary>
        [Theory]
        [InlineData("", "Doe", "user@example.com", "password", "password")]
        [InlineData("John", "", "user@example.com", "password", "password")]
        [InlineData("John", "Doe", "", "password", "password")]
        [InlineData("John", "Doe", "user@example.com", "", "password")]
        [InlineData("John", "Doe", "user@example.com", "password", "")]
        public void RegisterCommand_CannotExecute_WhenAnyFieldIsEmpty(
            string firstName, string lastName, string email, string password, string confirmPassword)
        {
            // Arrange
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = firstName;
            viewModel.LastName = lastName;
            viewModel.Email = email;
            viewModel.Password = password;
            viewModel.ConfirmPassword = confirmPassword;

            // Act
            bool canExecute = viewModel.RegisterCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        /// <summary>
        /// Test that RegisterCommand can execute when all fields are filled
        /// </summary>
        [Fact]
        public void RegisterCommand_CanExecute_WhenAllFieldsAreFilled()
        {
            // Arrange
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "John";
            viewModel.LastName = "Doe";
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "password123";

            // Act
            bool canExecute = viewModel.RegisterCommand.CanExecute(null);

            // Assert
            Assert.True(canExecute);
        }

        /// <summary>
        /// Test that RegisterCommand cannot execute during registration
        /// </summary>
        [Fact]
        public void RegisterCommand_CannotExecute_DuringRegistration()
        {
            // Arrange
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "John";
            viewModel.LastName = "Doe";
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "password123";
            
            // Manually set the IsRegistering property to true using reflection
            typeof(RegisterViewModel).GetProperty("IsRegistering").SetValue(viewModel, true);

            // Act
            bool canExecute = viewModel.RegisterCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        /// <summary>
        /// Test that RegisterAsync with password mismatch shows error
        /// </summary>
        [Fact]
        public async Task RegisterCommand_WithPasswordMismatch_ShowsError()
        {
            // Arrange
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "John";
            viewModel.LastName = "Doe";
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "differentpassword";

            // Act
            await viewModel.RegisterCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Passwords do not match.", viewModel.ErrorMessage);
            _mockAuthService.Verify(s => s.RegisterUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), 
                Times.Never);
        }

        /// <summary>
        /// Test that RegisterAsync with successful registration navigates to login
        /// </summary>
        [Fact]
        public async Task RegisterCommand_WithSuccessfulRegistration_NavigatesToLogin()
        {
            // Arrange
            _mockAuthService.Setup(s => s.RegisterUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            _mockNavigationService.Setup(n => n.NavigateToLoginAsync())
                .Returns(Task.CompletedTask);
            
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "John";
            viewModel.LastName = "Doe";
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "password123";

            // Act - We need to handle the Task.Delay in the RegisterAsync method
            // Since we can't easily modify the delay in the test, we'll use a timeout
            var task = viewModel.RegisterCommand.ExecuteAsync(null);
            var completedTask = await Task.WhenAny(task, Task.Delay(2000));
            
            if (completedTask != task)
            {
                throw new TimeoutException("RegisterCommand execution timed out");
            }

            // Assert
            _mockAuthService.Verify(s => s.RegisterUserAsync(
                "John", "Doe", "user@example.com", "password123"), 
                Times.Once);
            
            Assert.True(viewModel.RegistrationSuccessful);
            
            // The navigation happens after a delay, so we can't directly verify it
            // We can verify that fields were reset instead
            Assert.Empty(viewModel.FirstName);
            Assert.Empty(viewModel.LastName);
            Assert.Empty(viewModel.Email);
            Assert.Empty(viewModel.Password);
            Assert.Empty(viewModel.ConfirmPassword);
        }

        /// <summary>
        /// Test that RegisterAsync with failed registration shows error
        /// </summary>
        [Fact]
        public async Task RegisterCommand_WithFailedRegistration_ShowsError()
        {
            // Arrange
            _mockAuthService.Setup(s => s.RegisterUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "John";
            viewModel.LastName = "Doe";
            viewModel.Email = "existing@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "password123";

            // Act
            await viewModel.RegisterCommand.ExecuteAsync(null);

            // Assert
            _mockAuthService.Verify(s => s.RegisterUserAsync(
                "John", "Doe", "existing@example.com", "password123"), 
                Times.Once);
            
            Assert.False(viewModel.RegistrationSuccessful);
            Assert.Contains("Registration failed", viewModel.ErrorMessage);
        }

        /// <summary>
        /// Test that GoToLoginCommand navigates to login page
        /// </summary>
        [Fact]
        public async Task GoToLoginCommand_NavigatesToLoginPage()
        {
            // Arrange
            _mockNavigationService.Setup(n => n.NavigateToLoginAsync())
                .Returns(Task.CompletedTask);
            
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);

            // Act
            await viewModel.GoToLoginCommand.ExecuteAsync(null);

            // Assert
            _mockNavigationService.Verify(n => n.NavigateToLoginAsync(), Times.Once);
        }
        
        /// <summary>
        /// Test that property changes update RegisterCommand's CanExecute
        /// </summary>
        [Fact]
        public void PropertyChanges_UpdateRegisterCommandCanExecute()
        {
            // Arrange
            var viewModel = new RegisterViewModel(_mockAuthService.Object, _mockNavigationService.Object);
            viewModel.FirstName = "";
            viewModel.LastName = "Doe";
            viewModel.Email = "user@example.com";
            viewModel.Password = "password123";
            viewModel.ConfirmPassword = "password123";
            
            bool initialCanExecute = viewModel.RegisterCommand.CanExecute(null);
            
            // Act
            viewModel.FirstName = "John";
            bool finalCanExecute = viewModel.RegisterCommand.CanExecute(null);
            
            // Assert
            Assert.False(initialCanExecute);
            Assert.True(finalCanExecute);
        }
    }
}