using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", t =>
        {
            t.HasCheckConstraint("CK_Events_DisciplineType", "[DisciplineType] IN ('Running','Cycling','Walking')");
            t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Draft','Published','Cancelled','Completed')");
        });

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        // No explicit HasColumnType here: a string property with no
        // HasMaxLength already maps to NVARCHAR(MAX) on SQL Server by
        // convention. An explicit "nvarchar(max)" column type is SQL
        // Server-only syntax - SQLite's CREATE TABLE parser requires a
        // numeric argument in a type's parentheses and throws on "max".
        builder.Property(e => e.DisciplineType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EventDate).HasColumnType("date").IsRequired();
        builder.Property(e => e.StartTime).HasColumnType("time");
        builder.Property(e => e.Province).HasMaxLength(50);
        builder.Property(e => e.Venue).HasMaxLength(200);
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Published");
        builder.Property(e => e.BannerImageUrl).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(e => e.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .HasConstraintName("FK_Events_Organiser")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
