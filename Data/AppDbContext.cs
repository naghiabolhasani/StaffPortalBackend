using StaffPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace StaffPortal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Constant> Constants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== Schema =====
        modelBuilder.Entity<User>().ToTable("Users", "app");
        modelBuilder.Entity<Role>().ToTable("Roles", "app");
        modelBuilder.Entity<Permission>().ToTable("Permissions", "app");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles", "app");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermissions", "app");
        modelBuilder.Entity<Constant>().ToTable("Constant");

        // ===== Index =====
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();
    }
}