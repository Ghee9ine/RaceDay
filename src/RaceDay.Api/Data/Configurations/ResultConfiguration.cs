using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished','DNF','DSQ')"));

        builder.HasKey(r => r.ResultId);

        builder.Property(r => r.FinishTime).HasColumnType("time");
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Finished");
        builder.Property(r => r.CapturedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .HasConstraintName("FK_Results_Enrolment")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CapturedBy)
            .WithMany(u => u.CapturedResults)
            .HasForeignKey(r => r.CapturedByUserId)
            .HasConstraintName("FK_Results_CapturedBy")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
    }
}
