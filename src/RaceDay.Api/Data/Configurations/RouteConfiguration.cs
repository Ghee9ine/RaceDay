using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

/// <summary>Configures CourseRoute against the (unrenamed) Routes table.</summary>
public class RouteConfiguration : IEntityTypeConfiguration<CourseRoute>
{
    public void Configure(EntityTypeBuilder<CourseRoute> builder)
    {
        builder.ToTable("Routes");

        builder.HasKey(r => r.RouteId);

        builder.Property(r => r.StartPoint).HasMaxLength(200).IsRequired();
        builder.Property(r => r.EndPoint).HasMaxLength(200).IsRequired();
        builder.Property(r => r.MapUrl).HasMaxLength(300);

        builder.HasOne(r => r.Category)
            .WithOne(c => c.CourseRoute)
            .HasForeignKey<CourseRoute>(r => r.CategoryId)
            .HasConstraintName("FK_Routes_Category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CategoryId).IsUnique().HasDatabaseName("UQ_Routes_Category");
    }
}
