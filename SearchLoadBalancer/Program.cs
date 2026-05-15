using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using NLog.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SearchLoadBalancer.LoadBalancing;
using Shared.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddOpenApi();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "search-load-balancer",
        serviceInstanceId: Environment.GetEnvironmentVariable("LB_INSTANCE_ID") ?? $"search-load-balancer-{Environment.ProcessId}"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(LoadBalancerMetrics.MeterName)
            .AddPrometheusExporter();
    });
builder.Services.Configure<LoadBalancerOptions>(builder.Configuration.GetSection(LoadBalancerOptions.SectionName));
builder.Services.AddSingleton<IBackendScheduler, RoundRobinBackendScheduler>();
builder.Services.AddSingleton<IBackendScheduler, RandomBackendScheduler>();
builder.Services.AddSingleton<BackendSchedulerFactory>();
builder.Services.AddSingleton<LoadBalancerStatsStore>();
builder.Services.AddHttpClient("SearchBackend", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<LoadBalancerOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.BackendTimeoutSeconds));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var options = app.Services.GetRequiredService<IOptions<LoadBalancerOptions>>().Value;
NormalizeAndValidateOptions(options);

var scheduler = app.Services.GetRequiredService<BackendSchedulerFactory>().Create(options.Strategy);
var statsStore = app.Services.GetRequiredService<LoadBalancerStatsStore>();
statsStore.Initialize(options.Backends);

app.Logger.LogInformation(
    "Load balancer started with strategy '{Strategy}' and {BackendCount} backends.",
    scheduler.Name,
    options.Backends.Count);

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    strategy = scheduler.Name,
    backends = options.Backends.Select(x => new { x.Name, x.BaseUrl })
}))
.WithName("LoadBalancerHealth");

app.MapGet("/api/lb/stats", () => Results.Ok(statsStore.Snapshot(scheduler.Name)))
    .WithName("LoadBalancerStats");

app.MapPost("/api/search", async (
    SearchRequest request,
    IHttpClientFactory httpClientFactory,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest("Query must not be empty.");
    }

    statsStore.RecordIncomingRequest();

    var backends = options.Backends;
    var client = httpClientFactory.CreateClient("SearchBackend");
    var startIndex = scheduler.GetStartIndex(backends.Count);

    for (var attempt = 0; attempt < backends.Count; attempt++)
    {
        var backend = backends[(startIndex + attempt) % backends.Count];
        statsStore.RecordAttempt(backend);
        app.Logger.LogInformation("Routing request to backend {BackendName} ({BackendBaseUrl})", backend.Name, backend.BaseUrl);

        try
        {
            var backendUri = new Uri(new Uri(backend.BaseUrl.TrimEnd('/')), "/api/search");
            using var backendResponse = await client.PostAsJsonAsync(backendUri, request, cancellationToken);

            if ((int)backendResponse.StatusCode >= 500)
            {
                statsStore.RecordFailure(backend);
                continue;
            }

            SetRoutingHeaders(httpContext, scheduler.Name, backend, backendResponse);
            statsStore.RecordSuccess(backend);

            if ((int)backendResponse.StatusCode >= 400)
            {
                var errorBody = await backendResponse.Content.ReadAsStringAsync(cancellationToken);
                var contentType = backendResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
                return Results.Content(errorBody, contentType, statusCode: (int)backendResponse.StatusCode);
            }

            var result = await backendResponse.Content.ReadFromJsonAsync<SearchResult>(cancellationToken: cancellationToken);
            if (result is null)
            {
                statsStore.RecordFailure(backend);
                continue;
            }

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            statsStore.RecordFailure(backend);
            app.Logger.LogWarning(ex, "Backend '{BackendName}' failed on attempt {Attempt}.", backend.Name, attempt + 1);
        }
    }

    return Results.Problem(
        detail: "No search backend is currently reachable.",
        statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("LoadBalancedSearch");

app.MapPrometheusScrapingEndpoint();

app.Run();

static void SetRoutingHeaders(
    HttpContext context,
    string strategy,
    BackendEndpoint backend,
    HttpResponseMessage backendResponse)
{
    context.Response.Headers["X-LB-Strategy"] = strategy;
    context.Response.Headers["X-LB-Backend"] = $"{backend.Name} ({backend.BaseUrl})";

    if (backendResponse.Headers.TryGetValues("X-SearchApi-Instance", out var values))
    {
        var instance = values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(instance))
        {
            context.Response.Headers["X-SearchApi-Instance"] = instance;
        }
    }

    if (backendResponse.Headers.TryGetValues("X-Search-Cache", out var cacheValues))
    {
        var cacheStatus = cacheValues.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cacheStatus))
        {
            context.Response.Headers["X-Search-Cache"] = cacheStatus;
        }
    }
}

static void NormalizeAndValidateOptions(LoadBalancerOptions options)
{
    if (options.Backends.Count == 0)
    {
        throw new InvalidOperationException("LoadBalancer:Backends must contain at least one backend.");
    }

    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < options.Backends.Count; i++)
    {
        var backend = options.Backends[i];

        if (!Uri.TryCreate(backend.BaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid backend URL at index {i}: '{backend.BaseUrl}'.");
        }

        backend.BaseUrl = uri.ToString().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(backend.Name))
        {
            backend.Name = $"backend-{i + 1}";
        }

        if (!usedNames.Add(backend.Name))
        {
            throw new InvalidOperationException($"Duplicate backend name detected: '{backend.Name}'.");
        }
    }
}
