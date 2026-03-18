namespace Hyaena.Web;

using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

internal sealed class PollyForwarderHttpClientFactory : ForwarderHttpClientFactory
{
    private static readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy =
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromMilliseconds(700),
            });

    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        var retryHandler = new PolicyHttpMessageHandler(request =>
        {
            return IsRetryableMethod(request.Method)
                ? RetryPolicy
                : Policy.NoOpAsync<HttpResponseMessage>();
        })
        {
            InnerHandler = handler,
        };

        return retryHandler;
    }

    private static bool IsRetryableMethod(HttpMethod method)
        => method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;
}
