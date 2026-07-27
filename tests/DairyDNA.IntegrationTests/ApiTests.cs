using System.Net.Http.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DairyDNA.IntegrationTests;

public class DairyDnaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DairyDnaDbContext>));
            services.RemoveAll(typeof(DairyDnaDbContext));
            services.AddDbContext<DairyDnaDbContext>(o => o.UseInMemoryDatabase(_dbName));
            services.AddScoped<IDairyDnaDbContext>(sp => sp.GetRequiredService<DairyDnaDbContext>());
        });
    }
}

public class HealthTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public HealthTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }
}

public class ThinSliceHappyPathTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ThinSliceHappyPathTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Generate_summary_optimize_flow()
    {
        var genResponse = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        genResponse.EnsureSuccessStatusCode();
        var gen = await genResponse.Content.ReadFromJsonAsync<GenDto>();
        gen.Should().NotBeNull();

        var summary = await _client.GetFromJsonAsync<DemoDto>($"/api/demo/summary?generationId={gen!.id}");
        summary.Should().NotBeNull();
        summary!.inventory.Should().NotBeEmpty();
        summary.demand.Should().NotBeEmpty();

        var optResponse = await _client.PostAsJsonAsync("/api/optimization-runs", new { generationId = gen.id });
        optResponse.EnsureSuccessStatusCode();
        var opt = await optResponse.Content.ReadFromJsonAsync<OptDto>();
        opt.Should().NotBeNull();
        opt!.status.Should().BeOneOf("Feasible", "Infeasible");
        if (opt.movements is { Count: > 0 })
            opt.movements.Should().OnlyContain(m => m.expectedContributionMargin >= 0);
    }

    private sealed record GenDto(Guid id, string status);
    private sealed record DemoDto(List<object> inventory, List<object> demand);
    private sealed record OptDto(string status, decimal objectiveValue, List<MoveDto> movements);
    private sealed record MoveDto(decimal quantityPounds, decimal expectedContributionMargin, decimal transportationCost);
}
