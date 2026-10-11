
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BoidsSimulation.Web.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<UserSimulationPreferences> UserSimulationPreferences =>
        Set<UserSimulationPreferences>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserSimulationPreferences>(entity =>
        {
            entity.ToTable("UserSimulationPreferences");

            entity.HasKey(preferences => preferences.UserId);

            entity.Property(preferences => preferences.BoidColor)
                .HasMaxLength(7)
                .IsRequired();

            entity.Property(preferences => preferences.BackgroundColor)
                .HasMaxLength(7)
                .IsRequired();

            entity.Property(preferences => preferences.ShowTrails)
                .IsRequired();

            entity.Property(preferences => preferences.UpdatedAtUtc)
                .IsRequired();

            entity.HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<UserSimulationPreferences>(
                    preferences => preferences.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
