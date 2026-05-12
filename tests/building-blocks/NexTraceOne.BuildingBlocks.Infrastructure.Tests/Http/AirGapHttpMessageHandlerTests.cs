using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Infrastructure.Http;

using NSubstitute;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Http;

public sealed class AirGapHttpMessageHandlerTests
{
    private static IConfiguration BuildConfig(string mode)
    {
        var dict = new Dictionary<string, string?> { ["Platform:NetworkIsolation:Mode"] = mode };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static IServiceScopeFactory CreateMockServiceScopeFactory()
    {
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        serviceScope.ServiceProvider.Returns(serviceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);
        
        return serviceScopeFactory;
    }

    private static AirGapHttpMessageHandler CreateHandler(string mode)
    {
        var handler = new AirGapHttpMessageHandler(
            BuildConfig(mode),
            NullLogger<AirGapHttpMessageHandler>.Instance,
            CreateMockServiceScopeFactory())
        {
            InnerHandler = new PassthroughHandler(),
        };
        return handler;
    }

    [Fact]
    public async Task WhenModeIsOff_AllowsRequest()
    {
        var handler = CreateHandler("Off");
        var invoker = new HttpMessageInvoker(handler);

        var response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "http://example.com/api"),
            CancellationToken.None);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenModeIsRestricted_AllowsRequest()
    {
        var handler = CreateHandler("Restricted");
        var invoker = new HttpMessageInvoker(handler);

        var response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "http://example.com/api"),
            CancellationToken.None);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenModeIsAirGap_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler("AirGap");
        var invoker = new HttpMessageInvoker(handler);

        var act = () => invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "http://external-service.io/api"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*external-service.io*blocked*AirGap*");
    }

    [Fact]
    public async Task WhenModeIsAirGap_CaseInsensitive_Blocks()
    {
        var handler = CreateHandler("airgap");
        var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoker.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "http://openai.com/v1/chat"),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenModeIsAirGap_ThrowsWithDestinationInMessage()
    {
        var handler = CreateHandler("AirGap");
        var invoker = new HttpMessageInvoker(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoker.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "http://api.openai.com"),
                CancellationToken.None));

        ex.Message.Should().Contain("api.openai.com");
    }

    [Fact]
    public async Task WhenModeIsAbsent_AllowsRequest()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var handler = new AirGapHttpMessageHandler(
            config,
            NullLogger<AirGapHttpMessageHandler>.Instance,
            CreateMockServiceScopeFactory())
        {
            InnerHandler = new PassthroughHandler(),
        };
        var invoker = new HttpMessageInvoker(handler);

        var response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "http://example.com"),
            CancellationToken.None);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    private sealed class PassthroughHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
