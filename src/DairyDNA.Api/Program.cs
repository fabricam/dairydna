using DairyDNA.Api.Endpoints;
using DairyDNA.Application.Abstractions;
using DairyDNA.Application.Demo;
using DairyDNA.Application.Generation;
using DairyDNA.Application.Governance;
using DairyDNA.Application.Ingestion;
using DairyDNA.Application.Optimization;
using DairyDNA.Application.Replay;
using DairyDNA.Application.Scenarios;
using DairyDNA.Application.Transport;
using DairyDNA.DataGenerator;
using DairyDNA.DataIngestion;
using DairyDNA.Infrastructure;
using DairyDNA.Infrastructure.Persistence;
using DairyDNA.Optimization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("dairydna");
var useInMemory = builder.Configuration.GetValue("UseInMemoryDatabase", false)
                  || string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDairyDnaPersistence(connectionString, useInMemory);
builder.Services.AddScoped<SyntheticDataGenerator>();
builder.Services.AddScoped<ISyntheticDataGenerator>(sp => sp.GetRequiredService<SyntheticDataGenerator>());
builder.Services.AddScoped<IThinSliceGenerator>(sp => sp.GetRequiredService<SyntheticDataGenerator>());
builder.Services.AddScoped<IPublicDataImporter, PublicDataImporter>();
builder.Services.AddScoped<DairyDNA.Application.Forecasting.ISupplyForecastService, DairyDNA.Forecasting.MlNetSupplyForecastService>();
builder.Services.AddScoped<DairyDNA.Application.Forecasting.IDemandForecastService, DairyDNA.Forecasting.MlNetDemandForecastService>();
builder.Services.AddScoped<DairyDNA.Application.Forecasting.IPriceForecastService, DairyDNA.Forecasting.MlNetPriceForecastService>();
builder.Services.AddSingleton<ITransportCostCalculator, TransportCostCalculator>();
builder.Services.AddSingleton<OrToolsContributionMarginOptimizer>();
builder.Services.AddSingleton<NaiveContributionMarginOptimizer>();
builder.Services.AddSingleton<IAllocationOptimizer>(sp => sp.GetRequiredService<OrToolsContributionMarginOptimizer>());
builder.Services.AddSingleton<IAllocationOptimizerResolver, AllocationOptimizerResolver>();
builder.Services.AddScoped<CreateGenerationRunHandler>();
builder.Services.AddScoped<GetGenerationRunHandler>();
builder.Services.AddScoped<ListGenerationRunsHandler>();
builder.Services.AddScoped<GetValidationReportHandler>();
builder.Services.AddScoped<GetDemoSummaryHandler>();
builder.Services.AddScoped<DairyDNA.Application.Dashboard.GetDashboardHandler>();
builder.Services.AddScoped<CreateOptimizationRunHandler>();
builder.Services.AddScoped<GetOptimizationRunHandler>();
builder.Services.AddScoped<IScenarioService, ScenarioService>();
builder.Services.AddScoped<IModelGovernanceService, ModelGovernanceService>();
builder.Services.AddScoped<IReplayService, ReplayService>();
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
    var importer = scope.ServiceProvider.GetRequiredService<IPublicDataImporter>();
    await importer.EnsureSourcesSeededAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGenerationEndpoints();
app.MapDemoEndpoints();
app.MapDashboardEndpoints();
app.MapOptimizationEndpoints();
app.MapScenarioEndpoints();
app.MapReferenceEndpoints();
app.MapImportEndpoints();
app.MapForecastEndpoints();
app.MapTransportCostEndpoints();
app.MapModelGovernanceEndpoints();
app.MapReplayEndpoints();

app.Run();

public partial class Program;
