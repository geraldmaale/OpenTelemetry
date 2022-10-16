using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//using var loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddOpenTelemetry(options =>
//    {
//        options.AddConsoleExporter();
//    });
//});

//var logger = loggerFactory.CreateLogger<Program>();

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddOpenTelemetry(options =>
    {
        options.AddConsoleExporter();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    app.Logger.LogInformation("Fetched weather records");
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/test", () =>
{
    var name = "tomato";
    var price = 2.99;
    app.Logger.LogInformation("Hello from {name} {price}.", name, price);
    return Results.Ok($"Hello from {name} {price}");
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
