using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SignalGenerator.Data.Models;

namespace SignalGenerator.Data.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<SignalData> Signals { get; set; }
        public DbSet<SignalGroup> SignalGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure SignalData
            builder.Entity<SignalData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Frequency).IsRequired();
                entity.Property(e => e.Power).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.ProtocolType).IsRequired();
            });

            // Configure SignalConfig
            builder.Entity<SignalData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SignalCount).IsRequired();
                entity.Property(e => e.MinFrequency).IsRequired();
                entity.Property(e => e.MaxFrequency).IsRequired();
                entity.Property(e => e.IntervalMs).IsRequired();
                entity.Property(e => e.ProtocolType).IsRequired();
            });

            // Configure SignalGroup
            builder.Entity<SignalGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.CreatedById).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Members)
                    .WithMany(e => e.SignalGroups)
                    .UsingEntity(j => j.ToTable("SignalGroupMembers"));
            });
        }
    }
}


