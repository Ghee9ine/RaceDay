using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_Distance", "[DistanceKm] > 0"));

        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();
        builder.Property(c => c.EntryFee).HasColumnType("decimal(8,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(c => c.AgeGroup).HasMaxLength(30);

        builder.HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .HasConstraintName("FK_Categories_Event")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EventId, c.Name }).IsUnique().HasDatabaseName("UQ_Categories_EventName");
    }
}
