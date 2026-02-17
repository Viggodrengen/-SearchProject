using SearchApi.Search;
using Shared.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<SearchService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.MapPost("/api/search", (SearchRequest request, SearchService searchService) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest("Query must not be empty.");
    }

    if (!string.Equals(request.Database, "sqlite", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(request.Database, "postgres", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Database must be either 'sqlite' or 'postgres'.");
    }

    var result = searchService.Search(request);
    return Results.Ok(result);
})
.WithName("Search");

app.Run();
