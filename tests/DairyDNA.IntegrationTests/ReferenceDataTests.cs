using System.Net.Http.Json;
using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using DairyDNA.Domain.Enums;
using FluentAssertions;

namespace DairyDNA.IntegrationTests;

public class ReferenceDataTests : IClassFixture<DairyDnaApiFactory>
{
    private readonly HttpClient _client;

    public ReferenceDataTests(DairyDnaApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Network_includes_farms_facilities_customers()
    {
        var gen = await CreateGenerationAsync();
        var network = await _client.GetFromJsonAsync<NetworkDto>($"/api/network?generationId={gen}");
        network!.points.Should().Contain(p => p.kind == "Farm");
        network.points.Should().Contain(p => p.kind == "Facility");
        network.points.Should().Contain(p => p.kind == "Customer");
    }

    [Fact]
    public async Task Invalid_facility_create_is_rejected()
    {
        var gen = await CreateGenerationAsync();
        var response = await _client.PostAsJsonAsync("/api/facilities", new Facility
        {
            GenerationId = gen,
            Name = "Bad",
            FacilityType = FacilityType.Storage,
            RegionCode = "R1",
            Latitude = 43,
            Longitude = -89,
            MilkStorageCapacityPounds = -1,
            CreamStorageCapacityPounds = 0
        });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deactivate_customer_hides_from_default_list()
    {
        var gen = await CreateGenerationAsync();
        var customers = await _client.GetFromJsonAsync<List<Customer>>($"/api/customers?generationId={gen}&activeOnly=true");
        customers.Should().NotBeEmpty();
        var id = customers![0].Id;

        var deactivate = await _client.PostAsync($"/api/customers/{id}/deactivate", null);
        deactivate.EnsureSuccessStatusCode();

        var active = await _client.GetFromJsonAsync<List<Customer>>($"/api/customers?generationId={gen}&activeOnly=true");
        active!.Should().NotContain(c => c.Id == id);

        var detail = await _client.GetFromJsonAsync<Customer>($"/api/customers/{id}");
        detail!.Active.Should().BeFalse();

        var all = await _client.GetFromJsonAsync<List<Customer>>($"/api/customers?generationId={gen}&activeOnly=false");
        all!.Should().Contain(c => c.Id == id);
    }

    private async Task<Guid> CreateGenerationAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/generation-runs", new ThinSliceGenerationRequest());
        response.EnsureSuccessStatusCode();
        var gen = await response.Content.ReadFromJsonAsync<GenDto>();
        return gen!.id;
    }

    private sealed record GenDto(Guid id);
    private sealed record NetworkDto(List<PointDto> points);
    private sealed record PointDto(string kind);
}
