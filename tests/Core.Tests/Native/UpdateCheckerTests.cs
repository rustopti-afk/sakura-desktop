using Sakura.Core.Native;
using System.Net;
using System.Net.Http;

namespace Sakura.Core.Tests.Native;

public sealed class UpdateCheckerTests
{
    // ── GetCurrentVersion ──────────────────────────────────────────────────

    [Fact]
    public void GetCurrentVersion_ReturnsNonNullVersion()
    {
        var v = UpdateChecker.GetCurrentVersion();
        v.Should().NotBeNull();
    }

    [Fact]
    public void GetCurrentVersion_IsAtLeast_0_0_0()
    {
        var v = UpdateChecker.GetCurrentVersion();
        v.Should().BeGreaterThanOrEqualTo(new Version(0, 0, 0));
    }

    // ── CheckAsync with mock HTTP ──────────────────────────────────────────

    [Fact]
    public async Task CheckAsync_WhenNetworkFails_ReturnsNull()
    {
        using var client = new HttpClient(new FailingHandler());
        var result = await UpdateChecker.CheckAsync(client);
        result.Should().BeNull("network failures must be swallowed");
    }

    [Fact]
    public async Task CheckAsync_WhenResponseIsInvalidJson_ReturnsNull()
    {
        using var client = new HttpClient(new StaticResponseHandler("not json", HttpStatusCode.OK));
        var result = await UpdateChecker.CheckAsync(client);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenTagNameIsMalformed_ReturnsNull()
    {
        string json = """{"tag_name":"not-semver","html_url":"https://example.com","body":"notes"}""";
        using var client = new HttpClient(new StaticResponseHandler(json, HttpStatusCode.OK));
        var result = await UpdateChecker.CheckAsync(client);
        result.Should().BeNull("malformed version tag must return null");
    }

    [Fact]
    public async Task CheckAsync_WhenLatestIsHigher_ReturnsUpdateAvailable()
    {
        string json = """{"tag_name":"v99.0.0","html_url":"https://example.com/release","body":"fixes"}""";
        using var client = new HttpClient(new StaticResponseHandler(json, HttpStatusCode.OK));

        var result = await UpdateChecker.CheckAsync(client);

        result.Should().NotBeNull();
        result!.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("99.0.0");
        result.ReleasePageUrl.Should().Be("https://example.com/release");
        result.ReleaseNotes.Should().Be("fixes");
    }

    [Fact]
    public async Task CheckAsync_WhenLatestEqualsOrLower_ReturnsNotUpdateAvailable()
    {
        string json = """{"tag_name":"v0.0.1","html_url":"https://example.com","body":""}""";
        using var client = new HttpClient(new StaticResponseHandler(json, HttpStatusCode.OK));

        var result = await UpdateChecker.CheckAsync(client);

        result.Should().NotBeNull();
        result!.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_TagWithoutVPrefix_ParsesCorrectly()
    {
        string json = """{"tag_name":"99.0.0","html_url":"https://example.com","body":""}""";
        using var client = new HttpClient(new StaticResponseHandler(json, HttpStatusCode.OK));

        var result = await UpdateChecker.CheckAsync(client);
        result.Should().NotBeNull();
        result!.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("99.0.0");
    }

    [Fact]
    public async Task CheckAsync_CurrentVersionIsPopulated()
    {
        string json = """{"tag_name":"v99.0.0","html_url":"https://example.com","body":""}""";
        using var client = new HttpClient(new StaticResponseHandler(json, HttpStatusCode.OK));

        var result = await UpdateChecker.CheckAsync(client);
        result.Should().NotBeNull();
        result!.CurrentVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckAsync_WhenCancelled_ReturnsNull()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var client = new HttpClient(new DelayingHandler(TimeSpan.FromSeconds(10)));
        var result = await UpdateChecker.CheckAsync(client, cts.Token);
        result.Should().BeNull("cancelled check must return null");
    }

    // ── UpdateInfo record ──────────────────────────────────────────────────

    [Fact]
    public void UpdateInfo_Record_EqualityWorks()
    {
        var a = new UpdateInfo("1.0.0", "2.0.0", true, "https://x", "notes");
        var b = new UpdateInfo("1.0.0", "2.0.0", true, "https://x", "notes");
        a.Should().Be(b);
    }

    // ── Test helpers ───────────────────────────────────────────────────────

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            => throw new HttpRequestException("Simulated network failure");
    }

    private sealed class StaticResponseHandler(string body, HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class DelayingHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
