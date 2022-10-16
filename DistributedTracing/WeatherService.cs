using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DistributedTracing;

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


public static class WeatherService
{
    private static readonly Counter<int> GetWeatherCounter = TelemetryConstants.GetMeter.CreateCounter<int>(
        name: "get_weather_count", unit: "days", description: "The number of weather retrieved by days");

    private static int WeatherCount = 0;
    private static int WeatherCount2 = 0;


    public static void WeatherEndpoints(this WebApplication app)
    {

        TelemetryConstants.GetMeter.CreateObservableCounter<int>("fetched-weather", () => WeatherCount);
        TelemetryConstants.GetMeter.CreateObservableCounter<int>("fetched-weather-2", () => WeatherCount2);

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", () =>
        {
            using var activity = TelemetryConstants.MyActivitySource.StartActivity("GetWeatherForecast");

            // TelemetryConstants.GetWeatherForecastCounter.Inc();
            GetWeatherCounter.Add(1);
            WeatherCount++;
            WeatherCount2++;

            var rng = new Random();

            // Get the current time using the stopwatch
            var watch = new Stopwatch();

            Baggage.Current.SetBaggage("WeatherForecast", "GetRequest");

            try
            {
                watch.Start();

                Thread.Sleep(500);

                var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();

                // Get random weather forecast
                var random = Random.Shared.Next(0, 2);
                var result = random / random;

                // Record the time taken to get the weather forecast
                watch.Stop();
                TimeSpan ts = watch.Elapsed;
                //Log.Information("Weather forecast retrieved in {ElapsedMilliseconds}ms", watch.ElapsedMilliseconds);
                var ats = activity?.StartTimeUtc.Add(ts) - activity?.StartTimeUtc;

                //diagnosticContext.Set("UserId", "gematt");
                app.Logger.LogInformation("Successful!!!!!!!!!!!");

                // Recording the event
                activity?.AddEvent(new ActivityEvent("Get Forecast",
                    tags: new ActivityTagsCollection() { KeyValuePair.Create<string, object?>("forecast", forecast.Count()) }));

                return forecast;
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Error getting weather forecast");
                throw;
            }
            finally
            {
                activity?.SetTag("set-tag elapsed", watch.ElapsedMilliseconds);
                activity?.SetTag("UserId", "gematt");
            }
        })
        .WithName("GetWeatherForecast")
        .WithOpenApi();


        //app.MapGet("/weatherforecast/time", () =>
        //{

        //    using (Operation.Time("Get time taken to fetch weatherforecasts"))
        //    {
        //        var forecast = Enumerable.Range(1, 5).Select(index =>
        //            new WeatherForecast
        //            (
        //                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        //                Random.Shared.Next(-20, 55),
        //                summaries[Random.Shared.Next(summaries.Length)]
        //            ))
        //            .ToArray();

        //        //app.Logger.LogInformation("GetWeatherByDays {NumberDays}", forecast.Count());

        //        return forecast;
        //    }
        //})
        //.WithOpenApi();
    }
}
