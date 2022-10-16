using DistributedTracing;

var builder = WebApplication.CreateBuilder(args);


// Open OpenTelemetry
builder.AddOpenTelemetryCustomServices2();


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseSerilogCustomLoggingMiddleware();

app.UseHttpsRedirection();

// app.UsePrometheusMetrics();

// Add app endpoints
app.WeatherEndpoints();

app.Run();
