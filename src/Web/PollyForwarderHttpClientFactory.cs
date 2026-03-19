namespace Hyaena.Web;

using Polly;
using Polly.Retry;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

internal sealed class PollyForwarderHttpClientFactory : ForwarderHttpClientFactory
{
    private static readonly ResiliencePipeline<HttpResponseMessage> RetryPipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(static response =>
                        (int)response.StatusCode >= 500
                        || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 3,
                DelayGenerator = static args => new ValueTask<TimeSpan?>(args.AttemptNumber switch
                {
                    0 => TimeSpan.FromMilliseconds(100),
                    1 => TimeSpan.FromMilliseconds(300),
                    _ => TimeSpan.FromMilliseconds(700),
                }),
                UseJitter = false,
            })
            .Build();

    protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
    {
        return new RetryDelegatingHandler(handler);
    }

    private sealed class RetryDelegatingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!IsRetryableMethod(request.Method))
                return base.SendAsync(request, cancellationToken);

            return RetryPipeline.ExecuteAsync(
                ct => new ValueTask<HttpResponseMessage>(base.SendAsync(request, ct)),
                cancellationToken).AsTask();
        }

        private static bool IsRetryableMethod(HttpMethod method)
            => method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;
    }
}
