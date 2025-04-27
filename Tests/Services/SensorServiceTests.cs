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
        [Fact]
        public async Task StartAsync_ImmediateCancellation_DoesNotInvokeOnSensorUpdated()
        {
            var dummy = new Sensor { SensorId = 7 };
            var mockRepo = new Mock<ISensorRepository>();
            mockRepo.Setup(r => r.GetAllWithConfigurationAsync())
                    .ReturnsAsync(new List<Sensor> { dummy });

            var svc = new SensorService(mockRepo.Object);
            int invocationCount = 0;
            svc.OnSensorUpdated += (_, __) => invocationCount++;

            // Cancel before starting
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await svc.StartAsync(TimeSpan.FromMilliseconds(10), cts.Token);

            Assert.Equal(0, invocationCount);
        }

        [Fact]
        public async Task StartAsync_NullResults_DoesNotInvokeOnSensorUpdated()
        {
            var mockRepo = new Mock<ISensorRepository>();
            mockRepo.Setup(r => r.GetAllWithConfigurationAsync())
                    .ReturnsAsync((List<Sensor>?)null);

            var svc = new SensorService(mockRepo.Object);
            int invocationCount = 0;
            svc.OnSensorUpdated += (_, __) => invocationCount++;

            // We allow a single iteration then cancel
            var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            // Should not throw, just skip null
            await svc.StartAsync(TimeSpan.FromMilliseconds(10), cts.Token);

            Assert.Equal(0, invocationCount);
        }
    }
}
