using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.ClickHouse;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Tests.Runtime.Persistence.ClickHouse;

/// <summary>
/// Testes unitários para o ClickHouseRepository (stub implementation).
/// Cobre métodos de consulta de métricas de runtime.
/// </summary>
public sealed class ClickHouseRepositoryTests
{
    private readonly IClickHouseRepository _repository = new ClickHouseRepository("Host=localhost;Database=default");

    [Fact]
    public async Task GetRequestMetricsAsync_ShouldReturnEmptyList_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetRequestMetricsAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRequestMetricsAsync_ShouldFilterByEndpoint()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var endpoint = "/api/v1/test";

        // Act
        var result = await _repository.GetRequestMetricsAsync(from, to, endpoint);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetErrorAnalyticsAsync_ShouldReturnEmptyList_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetErrorAnalyticsAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetErrorAnalyticsAsync_ShouldFilterByErrorType()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var errorType = "System.Exception";

        // Act
        var result = await _repository.GetErrorAnalyticsAsync(from, to, errorType);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserActivityAsync_ShouldReturnEmptyList_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetUserActivityAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserActivityAsync_ShouldFilterByUserId()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var userId = "user-123";

        // Act
        var result = await _repository.GetUserActivityAsync(from, to, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldReturnEmptyList_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetSystemHealthAsync(from, to);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSystemHealthAsync_ShouldFilterByServiceName()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var serviceName = "my-service";

        // Act
        var result = await _repository.GetSystemHealthAsync(from, to, serviceName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAverageResponseTimeAsync_ShouldReturnZero_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetAverageResponseTimeAsync(from, to);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public async Task GetTotalRequestsAsync_ShouldReturnZero_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetTotalRequestsAsync(from, to);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetErrorRateAsync_ShouldReturnZero_WhenNoData()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetErrorRateAsync(from, to);

        // Assert
        result.Should().Be(0.0);
    }

    [Theory]
    [InlineData("production")]
    [InlineData("staging")]
    [InlineData("development")]
    public async Task GetSystemHealthAsync_ShouldSupportMultipleServiceNames(string serviceName)
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var result = await _repository.GetSystemHealthAsync(from, to, serviceName);

        // Assert
        result.Should().NotBeNull();
    }
}
