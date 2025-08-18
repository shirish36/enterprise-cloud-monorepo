using Google.Cloud.Diagnostics.AspNetCore3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Google Cloud Logging
var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
if (!string.IsNullOrEmpty(projectId))
{
    builder.Services.AddGoogleDiagnosticsForAspNetCore(projectId);
}

// Configure CORS for web app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWebApp");
app.UseAuthorization();

// Health check endpoint for Cloud Run
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Sample API endpoints
app.MapGet("/api/products", () => new[]
{
    new { Id = 1, Name = "Product 1", Price = 29.99 },
    new { Id = 2, Name = "Product 2", Price = 39.99 },
    new { Id = 3, Name = "Product 3", Price = 49.99 }
});

app.MapGet("/api/orders", () => new[]
{
    new { Id = 1, ProductId = 1, Quantity = 2, Total = 59.98 },
    new { Id = 2, ProductId = 2, Quantity = 1, Total = 39.99 }
});

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
