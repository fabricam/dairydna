using DairyDNA.Application.Abstractions;
using DairyDNA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DairyDNA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDairyDnaPersistence(this IServiceCollection services, string? connectionString, bool useInMemory = false)
    {
        if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<DairyDnaDbContext>(o => o.UseInMemoryDatabase("DairyDNA"));
        }
        else
        {
            services.AddDbContext<DairyDnaDbContext>(o => o.UseSqlServer(connectionString));
        }

        services.AddScoped<IDairyDnaDbContext>(sp => sp.GetRequiredService<DairyDnaDbContext>());
        return services;
    }
}
