namespace Hyaena.Web;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Model;

internal sealed class ReverseProxyHealthCheck(IProxyStateLookup proxyStateLookup, IOptions<ReverseProxyHealthCheckOptions> options) : IHealthCheck
{
    private readonly ReverseProxyHealthCheckOptions options = options.Value;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var clusters = proxyStateLookup
            .GetClusters()
            .Where(cluster => !options.ExcludedClusterIds.Contains(cluster.ClusterId))
            .ToArray();

        if (clusters.Length is 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No application reverse proxy clusters are loaded."));
        }

        var clusterSummaries = new List<string>();
        var hasUnhealthyCluster = false;
        var hasDegradedCluster = false;

        foreach (var cluster in clusters)
        {
            var destinations = cluster.Destinations.Values.ToArray();

            if (destinations.Length is 0)
            {
                hasUnhealthyCluster = true;
                clusterSummaries.Add($"{cluster.ClusterId}: no destinations");

                continue;
            }

            var destinationStates = destinations.Select(GetDestinationHealthState).ToArray();
            var healthyDestinationsCount = destinationStates.Count(state => state is DestinationAggregateHealth.Healthy);
            var unhealthyDestinationsCount = destinationStates.Count(state => state is DestinationAggregateHealth.Unhealthy);
            var unknownDestinationsCount = destinationStates.Count(state => state is DestinationAggregateHealth.Unknown);

            if (healthyDestinationsCount is 0)
            {
                hasUnhealthyCluster = true;
                clusterSummaries.Add($"{cluster.ClusterId}: unhealthy (0/{destinations.Length} healthy, {unknownDestinationsCount} unknown)");
            }
            else if (unhealthyDestinationsCount > 0 || (destinations.Length > 1 && unknownDestinationsCount > 0))
            {
                hasDegradedCluster = true;
                clusterSummaries.Add($"{cluster.ClusterId}: degraded ({healthyDestinationsCount}/{destinations.Length} healthy, {unknownDestinationsCount} unknown)");
            }
            else
            {
                clusterSummaries.Add($"{cluster.ClusterId}: healthy ({healthyDestinationsCount}/{destinations.Length} healthy)");
            }
        }

        var description = string.Join("; ", clusterSummaries);

        if (hasUnhealthyCluster)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(description));
        }

        if (hasDegradedCluster)
        {
            return Task.FromResult(HealthCheckResult.Degraded(description));
        }

        return Task.FromResult(HealthCheckResult.Healthy(description));
    }

    private static DestinationAggregateHealth GetDestinationHealthState(DestinationState destinationState)
    {
        var activeHealth = destinationState.Health.Active;
        var passiveHealth = destinationState.Health.Passive;

        if (activeHealth is DestinationHealth.Unhealthy || passiveHealth is DestinationHealth.Unhealthy)
        {
            return DestinationAggregateHealth.Unhealthy;
        }

        if (activeHealth is DestinationHealth.Unknown && passiveHealth is DestinationHealth.Unknown)
        {
            return DestinationAggregateHealth.Unknown;
        }

        return DestinationAggregateHealth.Healthy;
    }
}
