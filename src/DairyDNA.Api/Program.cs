using DairyDNA.Api.Endpoints;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Demo;
using DairyDNA.Application.Generation;
using DairyDNA.Application.Optimization;
using DairyDNA.Application.Transport;
using DairyDNA.DataGenerator;
using DairyDNA.Infrastructure;
using DairyDNA.Infrastructure.Persistence;
using DairyDNA.Optimization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("dairydna");
var useInMemory = builder.Configuration.GetValue("UseInMemoryDatabase", false)
                  || string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDairyDnaPersistence(connectionString, useInMemory);
builder.Services.AddScoped<IThinSliceGenerator, ThinSliceGenerator>();
builder.Services.AddSingleton<ITransportCostCalculator, TransportCostCalculator>();
builder.Services.AddSingleton<IAllocationOptimizer, NaiveContributionMarginOptimizer>();
builder.Services.AddScoped<CreateGenerationRunHandler>();
builder.Services.AddScoped<GetGenerationRunHandler>();
builder.Services.AddScoped<ListGenerationRunsHandler>();
builder.Services.AddScoped<GetDemoSummaryHandler>();
builder.Services.AddScoped<CreateOptimizationRunHandler>();
builder.Services.AddScoped<GetOptimizationRunHandler>();
builder.Services.AddScoped<DairyDNA.Application.Reference.ReferenceDataHandlers>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DairyDnaDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGenerationEndpoints();
app.MapDemoEndpoints();
app.MapOptimizationEndpoints();
app.MapReferenceEndpoints();

app.Run();

public partial class Program;
