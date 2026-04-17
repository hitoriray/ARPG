using ARPGServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ARPGServer.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<CloudSave> CloudSaves => Set<CloudSave>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.UserName).IsUnique();
            entity.Property(user => user.UserName).HasMaxLength(32).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<CloudSave>(entity =>
        {
            entity.HasKey(save => save.Id);
            entity.HasIndex(save => save.UserId).IsUnique();
            entity.Property(save => save.SaveJson).IsRequired();
            entity.Property(save => save.Version).IsRequired();
            entity.Property(save => save.UpdatedAtUtc).IsRequired();

            entity.HasOne(save => save.User)
                .WithOne(user => user.CloudSave)
                .HasForeignKey<CloudSave>(save => save.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
