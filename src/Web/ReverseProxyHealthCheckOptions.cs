namespace Hyaena.Web;

internal sealed class ReverseProxyHealthCheckOptions
{
    public HashSet<string> ExcludedClusterIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
