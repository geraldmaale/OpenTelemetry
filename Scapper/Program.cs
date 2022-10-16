using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MyCompany.MyProduct.MyLibrary")
            .AddConsoleExporter()
            .AddPrometheusHttpListener(opt =>
            {
                opt.UriPrefixes = new[] { "http://localhost:9090" };
            })
            .Build();
while (!Console.KeyAvailable)
{
    MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
    MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
    MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
    MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
    MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
    MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));
}

Console.ReadLine();
