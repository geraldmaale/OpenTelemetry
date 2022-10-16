using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace DistributedTracing
{
    public class TelemetryConstants
    {
        public static readonly string ServiceName = Assembly.GetExecutingAssembly().GetName().Name?.ToString()!;
        public static readonly string ServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()!;

        public static readonly ActivitySource MyActivitySource = new(ServiceName, ServiceVersion);

        public static readonly Meter GetMeter = new(ServiceName, ServiceVersion);
    }
}
