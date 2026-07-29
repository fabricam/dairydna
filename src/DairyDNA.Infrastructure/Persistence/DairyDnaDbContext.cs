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
    }
}
