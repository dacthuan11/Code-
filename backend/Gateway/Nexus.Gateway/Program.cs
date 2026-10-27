using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opt => opt.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors("AllowAll");

var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
string ServiceUrl(string dockerHost, int localPort) =>
    isDocker ? ("http://" + dockerHost) : ("http://localhost:" + localPort);

// Map: path tai gateway -> (URL downstream, ten hien thi)
var swaggerDocs = new Dictionary<string, (string downstreamUrl, string displayName)>
{
    ["/swagger/user/v1/swagger.json"]         = (ServiceUrl("user-service", 5001) + "/swagger/v1/swagger.json", "👤 UserService"),
    ["/swagger/catalog/v1/swagger.json"]      = (ServiceUrl("catalog-service", 5002) + "/swagger/v1/swagger.json", "📦 CatalogService"),
    ["/swagger/commerce/v1/swagger.json"]     = (ServiceUrl("commerce-service", 5003) + "/swagger/v1/swagger.json", "🛒 CommerceService"),
    ["/swagger/auction/v1/swagger.json"]      = (ServiceUrl("auction-service", 5004) + "/swagger/v1/swagger.json", "🔨 AuctionService"),
    ["/swagger/fulfillment/v1/swagger.json"]  = (ServiceUrl("fulfillment-service", 5005) + "/swagger/v1/swagger.json", "🚚 FulfillmentService"),
    ["/swagger/notification/v1/swagger.json"] = (ServiceUrl("notification-service", 5006) + "/swagger/v1/swagger.json", "🔔 NotificationService"),
};

// Middleware chay TRUOC Ocelot: xu ly /health va proxy swagger.json tu cac services
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";

    // Health check
    if (path == "/health")
    {
        ctx.Response.StatusCode = 200;
        await ctx.Response.WriteAsync("Gateway Healthy");
        return;
    }

    // Proxy swagger.json cua moi service ve client (tranh CORS tu browser)
    if (swaggerDocs.TryGetValue(path, out var doc))
    {
        var http = ctx.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
        try
        {
            var json = await http.GetStringAsync(doc.downstreamUrl);
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(json);
        }
        catch
        {
            ctx.Response.StatusCode = 503;
            ctx.Response.ContentType = "application/json";
            var errJson = "{\"error\":\"" + doc.displayName + " hien khong kha dung\"}";
            await ctx.Response.WriteAsync(errJson);
        }
        return;
    }

    await next();
});

// Swagger UI tong hop - tat ca 6 services trong 1 trang
app.UseSwaggerUI(c =>
{
    foreach (var (path, (_, name)) in swaggerDocs)
        c.SwaggerEndpoint(path, name);

    c.RoutePrefix        = "swagger";
    c.DocumentTitle      = "Nexus API Gateway — All Services";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

await app.UseOcelot();
app.Run();
