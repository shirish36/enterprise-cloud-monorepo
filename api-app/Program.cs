using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add minimal services (Controllers could be added later)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure dynamic port for Cloud Run (PORT env var) + local dev default 8080
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

if (app.Environment.IsDevelopment())
{
	 app.UseSwagger();
	 app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new { message = "api-app running", timeUtc = DateTime.UtcNow }))
	.WithName("Root");

app.MapGet("/healthz", () => Results.Ok("ok"))
	.WithName("Healthz")
	.Produces<string>(StatusCodes.Status200OK);

app.Run();

