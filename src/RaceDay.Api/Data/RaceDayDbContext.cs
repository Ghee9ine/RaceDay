using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core Code-First context for RaceDay. Table/column shape is driven by
/// the Fluent API configuration classes in Data/Configurations, which are
/// written to match docs/raceday-schema.sql exactly (see
/// docs/ef-generated-schema.sql for the generated-vs-plan parity check).
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CourseRoute> Routes => Set<CourseRoute>();
    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaceDayDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
