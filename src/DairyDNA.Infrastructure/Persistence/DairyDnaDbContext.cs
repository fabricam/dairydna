using DairyDNA.Application.Abstractions;
using DairyDNA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DairyDNA.Infrastructure.Persistence;

public sealed class DairyDnaDbContext : DbContext, IDairyDnaDbContext
{
    public DairyDnaDbContext(DbContextOptions<DairyDnaDbContext> options) : base(options) { }

    public DbSet<GenerationManifest> GenerationManifests => Set<GenerationManifest>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<OptimizationRun> OptimizationRuns => Set<OptimizationRun>();
    public DbSet<RecommendedMovement> RecommendedMovements => Set<RecommendedMovement>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<ImportSource> ImportSources => Set<ImportSource>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    public DbSet<RawPayload> RawPayloads => Set<RawPayload>();
    public DbSet<QuarantineItem> QuarantineItems => Set<QuarantineItem>();
    public DbSet<PublicMarketPrice> PublicMarketPrices => Set<PublicMarketPrice>();
    public DbSet<PublicWeatherObservation> PublicWeatherObservations => Set<PublicWeatherObservation>();
    public DbSet<FuelPriceObservation> FuelPriceObservations => Set<FuelPriceObservation>();
    public DbSet<SupplyModelVersion> SupplyModelVersions => Set<SupplyModelVersion>();
    public DbSet<SupplyFeatureSnapshot> SupplyFeatureSnapshots => Set<SupplyFeatureSnapshot>();
    public DbSet<SupplyForecast> SupplyForecasts => Set<SupplyForecast>();
    public DbSet<DemandModelVersion> DemandModelVersions => Set<DemandModelVersion>();
    public DbSet<DemandFeatureSnapshot> DemandFeatureSnapshots => Set<DemandFeatureSnapshot>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();

    IQueryable<GenerationManifest> IDairyDnaDbContext.GenerationManifests => GenerationManifests;
    IQueryable<Farm> IDairyDnaDbContext.Farms => Farms;
    IQueryable<Facility> IDairyDnaDbContext.Facilities => Facilities;
    IQueryable<Product> IDairyDnaDbContext.Products => Products;
    IQueryable<InventoryLot> IDairyDnaDbContext.InventoryLots => InventoryLots;
    IQueryable<Customer> IDairyDnaDbContext.Customers => Customers;
    IQueryable<Order> IDairyDnaDbContext.Orders => Orders;
    IQueryable<Truck> IDairyDnaDbContext.Trucks => Trucks;
    IQueryable<MarketPrice> IDairyDnaDbContext.MarketPrices => MarketPrices;
    IQueryable<OptimizationRun> IDairyDnaDbContext.OptimizationRuns => OptimizationRuns;
    IQueryable<RecommendedMovement> IDairyDnaDbContext.RecommendedMovements => RecommendedMovements;
    IQueryable<Contract> IDairyDnaDbContext.Contracts => Contracts;
    IQueryable<Shipment> IDairyDnaDbContext.Shipments => Shipments;
    IQueryable<WeatherObservation> IDairyDnaDbContext.WeatherObservations => WeatherObservations;
    IQueryable<ImportSource> IDairyDnaDbContext.ImportSources => ImportSources;
    IQueryable<ImportRun> IDairyDnaDbContext.ImportRuns => ImportRuns;
    IQueryable<RawPayload> IDairyDnaDbContext.RawPayloads => RawPayloads;
    IQueryable<QuarantineItem> IDairyDnaDbContext.QuarantineItems => QuarantineItems;
    IQueryable<PublicMarketPrice> IDairyDnaDbContext.PublicMarketPrices => PublicMarketPrices;
    IQueryable<PublicWeatherObservation> IDairyDnaDbContext.PublicWeatherObservations => PublicWeatherObservations;
    IQueryable<FuelPriceObservation> IDairyDnaDbContext.FuelPriceObservations => FuelPriceObservations;
    IQueryable<SupplyModelVersion> IDairyDnaDbContext.SupplyModelVersions => SupplyModelVersions;
    IQueryable<SupplyFeatureSnapshot> IDairyDnaDbContext.SupplyFeatureSnapshots => SupplyFeatureSnapshots;
    IQueryable<SupplyForecast> IDairyDnaDbContext.SupplyForecasts => SupplyForecasts;
    IQueryable<DemandModelVersion> IDairyDnaDbContext.DemandModelVersions => DemandModelVersions;
    IQueryable<DemandFeatureSnapshot> IDairyDnaDbContext.DemandFeatureSnapshots => DemandFeatureSnapshots;
    IQueryable<DemandForecast> IDairyDnaDbContext.DemandForecasts => DemandForecasts;

    public new void Add<T>(T entity) where T : class => Set<T>().Add(entity);
    public void AddRange<T>(IEnumerable<T> entities) where T : class => Set<T>().AddRange(entities);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GenerationManifest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ScenarioName).HasMaxLength(200);
            e.Property(x => x.SchemaVersion).HasMaxLength(100);
        });
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GenerationId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<InventoryLot>().HasKey(x => x.Id);
        modelBuilder.Entity<Order>().HasKey(x => x.Id);
        modelBuilder.Entity<RecommendedMovement>().HasKey(x => x.Id);
        modelBuilder.Entity<OptimizationRun>().HasKey(x => x.Id);
        modelBuilder.Entity<Contract>().HasKey(x => x.Id);
        modelBuilder.Entity<Shipment>().HasKey(x => x.Id);
        modelBuilder.Entity<WeatherObservation>().HasKey(x => x.Id);
        modelBuilder.Entity<ImportSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<ImportRun>().HasKey(x => x.Id);
        modelBuilder.Entity<RawPayload>().HasKey(x => x.Id);
        modelBuilder.Entity<QuarantineItem>().HasKey(x => x.Id);
        modelBuilder.Entity<PublicMarketPrice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ImportRunId, x.ProductCode, x.RegionCode, x.EffectiveDate });
        });
        modelBuilder.Entity<PublicWeatherObservation>().HasKey(x => x.Id);
        modelBuilder.Entity<FuelPriceObservation>().HasKey(x => x.Id);
        modelBuilder.Entity<SupplyModelVersion>().HasKey(x => x.Id);
        modelBuilder.Entity<SupplyFeatureSnapshot>().HasKey(x => x.Id);
        modelBuilder.Entity<SupplyForecast>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GenerationId, x.ModelVersionId, x.FacilityId, x.HorizonDays, x.TargetDate });
        });
        modelBuilder.Entity<DemandModelVersion>().HasKey(x => x.Id);
        modelBuilder.Entity<DemandFeatureSnapshot>().HasKey(x => x.Id);
        modelBuilder.Entity<DemandForecast>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.GenerationId, x.ModelVersionId, x.CustomerId, x.HorizonDays, x.TargetDate });
        });
    }
}
