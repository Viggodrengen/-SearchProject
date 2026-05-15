using System.Diagnostics;
using NLog.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SearchApi.Services;
using Shared.Model;

var builder = WebApplication.CreateBuilder(args);
var instanceId = builder.Configuration["SearchApi:InstanceId"]
    ?? Environment.GetEnvironmentVariable("SEARCH_INSTANCE_ID")
    ?? $"search-api-{Environment.ProcessId}";

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddOpenApi();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "search-api",
        serviceInstanceId: instanceId))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(SearchMetrics.MeterName)
            .AddPrometheusExporter();
    });
builder.Services.Configure<SearchCacheOptions>(builder.Configuration.GetSection(SearchCacheOptions.SectionName));
var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "SearchProject_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSingleton<SearchService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    app.Logger.LogInformation(
        "HTTP {Method} {Path} => {StatusCode} in {ElapsedMs} ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds);
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", instanceId }))
    .WithName("Health");

app.MapPost("/api/search", async (
    SearchRequest request,
    SearchService searchService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers["X-SearchApi-Instance"] = instanceId;

    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest("Query must not be empty.");
    }

    if (!string.Equals(request.Database, "sqlite", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(request.Database, "postgres", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Database must be either 'sqlite' or 'postgres'.");
    }

    var response = await searchService.SearchAsync(request, cancellationToken);
    httpContext.Response.Headers["X-Search-Cache"] = response.CacheStatus;

    return Results.Ok(response.Result);
})
.WithName("Search");

app.MapPrometheusScrapingEndpoint();

app.Run();
