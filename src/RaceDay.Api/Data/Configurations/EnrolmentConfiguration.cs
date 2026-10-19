using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed','Cancelled')"));

        builder.HasKey(e => e.EnrolmentId);

        builder.Property(e => e.BibNumber).HasMaxLength(10);
        builder.Property(e => e.EnrolmentDate).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Confirmed");

        builder.HasOne(e => e.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .HasConstraintName("FK_Enrolments_Participant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .HasConstraintName("FK_Enrolments_Category")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique().HasDatabaseName("UQ_Enrolments_ParticipantCategory");
    }
}
