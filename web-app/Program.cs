using Google.Cloud.Diagnostics.AspNetCore3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// Add Google Cloud Logging
var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
if (!string.IsNullOrEmpty(projectId))
{
    builder.Services.AddGoogleDiagnosticsForAspNetCore(projectId);
}

// Configure API client
builder.Services.AddHttpClient("ApiClient", client =>
{
    var apiUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:8080";
    client.BaseAddress = new Uri(apiUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Health check endpoint for Cloud Run
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Simple web interface
app.MapGet("/", async (HttpContext context) =>
{
    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>Enterprise Web App</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .container {{ max-width: 800px; margin: 0 auto; }}
        .card {{ border: 1px solid #ddd; padding: 20px; margin: 10px 0; border-radius: 5px; }}
        button {{ background: #4285f4; color: white; border: none; padding: 10px 20px; border-radius: 3px; cursor: pointer; }}
        button:hover {{ background: #3367d6; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Enterprise Web Application</h1>
        <div class='card'>
            <h2>System Status</h2>
            <p>Application is running on Google Cloud Run</p>
            <p>Environment: Production</p>
            <p>Timestamp: {timestamp}</p>
        </div>
        <div class='card'>
            <h2>API Integration</h2>
            <button onclick='loadProducts()'>Load Products</button>
            <button onclick='loadOrders()'>Load Orders</button>
            <div id='data'></div>
        </div>
    </div>
    <script>
        async function loadProducts() {{
            try {{
                const response = await fetch('/api/products');
                const products = await response.json();
                document.getElementById('data').innerHTML = '<h3>Products:</h3>' + 
                    JSON.stringify(products, null, 2);
            }} catch (error) {{
                document.getElementById('data').innerHTML = 'Error loading products: ' + error.message;
            }}
        }}
        async function loadOrders() {{
            try {{
                const response = await fetch('/api/orders');
                const orders = await response.json();
                document.getElementById('data').innerHTML = '<h3>Orders:</h3>' + 
                    JSON.stringify(orders, null, 2);
            }} catch (error) {{
                document.getElementById('data').innerHTML = 'Error loading orders: ' + error.message;
            }}
        }}
    </script>
</body>
</html>";
    
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
});

// Proxy API endpoints
app.MapGet("/api/products", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("ApiClient");
    try
    {
        var response = await client.GetStringAsync("/api/products");
        return Results.Content(response, "application/json");
    }
    catch
    {
        return Results.Json(new[] { new { Id = 0, Name = "API Unavailable", Price = 0.0 } });
    }
});

app.MapGet("/api/orders", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient("ApiClient");
    try
    {
        var response = await client.GetStringAsync("/api/orders");
        return Results.Content(response, "application/json");
    }
    catch
    {
        return Results.Json(new[] { new { Id = 0, ProductId = 0, Quantity = 0, Total = 0.0, Error = "API Unavailable" } });
    }
});

app.MapRazorPages();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
