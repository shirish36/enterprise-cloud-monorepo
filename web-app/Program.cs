var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.MapGet("/", () => Results.Content(@"<html><body><h1>web-app</h1><p>Running on Cloud Run compatible container.</p></body></html>", "text/html"));
app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();

