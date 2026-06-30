using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpMetrics();

app.MapGet("/", () => Results.Ok(new
{
    app = "DemoApi",
    status = "running",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.MapGet("/api/products", () =>
{
    var products = new[]
    {
        new { Id = 1, Name = "Keyboard", Price = 25000 },
        new { Id = 2, Name = "Mouse", Price = 12000 },
        new { Id = 3, Name = "Monitor", Price = 95000 }
    };

    return Results.Ok(products);
});

app.MapMetrics();

app.Run();

public partial class Program { }