using OpenTelemetry;
using OpenTelemetry.Trace;

static class LoggerExtensions
{
    public static TracerProviderBuilder AddMyConsoleExporter(this TracerProviderBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddProcessor(new BatchActivityExportProcessor(new MyConsoleExporter()));
    }



}