using Moq;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;


namespace SET09102_2024_5.Tests.Services
{
    public class SensorServiceTests
    {
        [Fact]
        public async Task GetAllWithConfigurationAsync_ProxiesRepository()
        {
            var mockRepo = new Mock<ISensorRepository>();
            var sensors = new List<Sensor> { new Sensor { SensorId = 1 } };
            mockRepo.Setup(r => r.GetAllWithConfigurationAsync())
                    .ReturnsAsync(sensors)
                    .Verifiable();

            var svc = new SensorService(mockRepo.Object);
            var result = await svc.GetAllWithConfigurationAsync();

            mockRepo.Verify();
            Assert.Same(sensors, result);
        }

        [Fact]
        public async Task StartAsync_InvokesOnSensorUpdatedOnceThenStops()
        {
            // Arrange
            var dummy = new Sensor { SensorId = 7 };
            var mockRepo = new Mock<ISensorRepository>();

            // First call returns a list containing 'dummy',
            // second call throws to break out of the loop.
            mockRepo.SetupSequence(r => r.GetAllWithConfigurationAsync())
                    .ReturnsAsync(new List<Sensor> { dummy })
                    .ThrowsAsync(new InvalidOperationException("stop"));

            var svc = new SensorService(mockRepo.Object);
            int invocationCount = 0;
            svc.OnSensorUpdated += (s, _) =>
            {
                if (s.SensorId == 7)
                    invocationCount++;
            };

            // Act & Assert (pass CancellationToken.None)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.StartAsync(TimeSpan.Zero, CancellationToken.None));
            Assert.Equal("stop", ex.Message);

            // Only one update should have fired before the exception
            Assert.Equal(1, invocationCount);
        }
    }
}
