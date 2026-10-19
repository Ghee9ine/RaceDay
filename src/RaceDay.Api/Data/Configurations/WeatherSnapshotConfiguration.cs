using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class WeatherSnapshotConfiguration : IEntityTypeConfiguration<WeatherSnapshot>
{
    public void Configure(EntityTypeBuilder<WeatherSnapshot> builder)
    {
        builder.ToTable("WeatherSnapshots");

        builder.HasKey(w => w.WeatherSnapshotId);

        builder.Property(w => w.RecordedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(w => w.TemperatureC).HasColumnType("decimal(4,1)");
        builder.Property(w => w.WindSpeedKmh).HasColumnType("decimal(5,1)");
        builder.Property(w => w.ConditionDescription).HasMaxLength(100);
        builder.Property(w => w.Source).HasMaxLength(50).HasDefaultValue("OpenWeatherMap");

        builder.HasOne(w => w.Event)
            .WithMany(e => e.WeatherSnapshots)
            .HasForeignKey(w => w.EventId)
            .HasConstraintName("FK_WeatherSnapshots_Event")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
