using AdmissionSystem.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdmissionSystem.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AdmissionClass> Classes => Set<AdmissionClass>();
    public DbSet<Hold> Holds => Set<Hold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionClass>(entity =>
        {
            entity.ToTable("Classes", table =>
            {
                table.HasCheckConstraint("CK_Classes_Name_NotEmpty", "length(btrim(\"Name\")) > 0");
                table.HasCheckConstraint("CK_Classes_TotalSeats_Positive", "\"TotalSeats\" > 0");
            });
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Hold>(entity =>
        {
            entity.ToTable("Holds", table =>
            {
                table.HasCheckConstraint("CK_Holds_StudentId_NotEmpty", "length(btrim(\"StudentId\")) > 0");
                table.HasCheckConstraint(
                    "CK_Holds_Status_Valid",
                    "\"Status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            });
            entity.HasKey(h => h.Id);
            entity.Property(h => h.StudentId).HasMaxLength(32).IsRequired();
            entity.Property(h => h.Status).HasMaxLength(16).IsRequired();

            entity.HasOne(h => h.Class)
                .WithMany(c => c.Holds)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            // Speeds up the seat-count queries.
            entity.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt });
            entity.HasIndex(h => h.StudentId);

            // Safety net: a student can never have two HELD/CONFIRMED rows
            // in the same class, even if the application code had a bug.
            entity.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasDatabaseName("IX_Holds_ClassId_StudentId_Active")
                .HasFilter("\"Status\" IN ('HELD', 'CONFIRMED')");
        });
    }
}
