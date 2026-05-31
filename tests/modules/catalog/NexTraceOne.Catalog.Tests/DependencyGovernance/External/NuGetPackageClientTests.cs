using System.Net;
using System.Net.Http;

using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.External;

namespace NexTraceOne.Catalog.Tests.DependencyGovernance.External;

public sealed class NuGetPackageClientTests
{
    private static HttpClient CreateClient(HttpResponseMessage response)
    {
        var handler = new TestHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.nuget.org/v3/") };
    }

    [Fact]
    public async Task GetLatestStableVersionAsync_ReturnsLatestVersion()
    {
        var json = "{ \"versions\": [ \"1.0.0\", \"1.1.0-beta\", \"2.0.0\", \"1.2.0\" ] }";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        var client = new NuGetPackageClient(CreateClient(response), NullLogger<NuGetPackageClient>.Instance);
        var result = await client.GetLatestStableVersionAsync("TestPackage");

        result.Should().Be("2.0.0");
    }

    [Fact]
    public async Task GetLatestStableVersionAsync_PackageNotFound_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var client = new NuGetPackageClient(CreateClient(response), NullLogger<NuGetPackageClient>.Instance);

        var result = await client.GetLatestStableVersionAsync("NonExistentPackage");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDeprecationInfoAsync_DeprecatedPackage_ReturnsTrue()
    {
        var json = @"{ ""catalogEntry"": { ""deprecation"": { ""message"": ""This package is deprecated."" } } }";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        var client = new NuGetPackageClient(CreateClient(response), NullLogger<NuGetPackageClient>.Instance);
        var (isDeprecated, message) = await client.GetDeprecationInfoAsync("TestPackage", "1.0.0");

        isDeprecated.Should().BeTrue();
        message.Should().Be("This package is deprecated.");
    }

    [Fact]
    public async Task GetDeprecationInfoAsync_NotDeprecated_ReturnsFalse()
    {
        var json = "{ \"catalogEntry\": { \"licenseExpression\": \"MIT\" } }";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        var client = new NuGetPackageClient(CreateClient(response), NullLogger<NuGetPackageClient>.Instance);
        var (isDeprecated, message) = await client.GetDeprecationInfoAsync("TestPackage", "1.0.0");

        isDeprecated.Should().BeFalse();
    }

    [Fact]
    public async Task GetLicenseAsync_ReturnsLicense()
    {
        var json = "{ \"catalogEntry\": { \"licenseExpression\": \"MIT\" } }";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        var client = new NuGetPackageClient(CreateClient(response), NullLogger<NuGetPackageClient>.Instance);
        var result = await client.GetLicenseAsync("TestPackage", "1.0.0");

        result.Should().Be("MIT");
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public TestHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
