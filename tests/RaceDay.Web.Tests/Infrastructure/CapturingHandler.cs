namespace RaceDay.Web.Tests.Infrastructure;

/// <summary>Terminal HttpMessageHandler that records the last request it saw and returns a
/// canned response, so a DelegatingHandler under test can be driven through a real HttpClient
/// pipeline without any actual network call.</summary>
public class CapturingHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public HttpResponseMessage ResponseToReturn { get; set; } = new(System.Net.HttpStatusCode.OK);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(ResponseToReturn);
    }
}
