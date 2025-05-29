using Microsoft.EntityFrameworkCore;
using MenuManagement.Domain.Entities;

namespace MenuManagement.Infrastructure.Data
{
    /// <summary>
    /// Contexto de base de datos para la aplicación
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RoleMenuPermission> RoleMenuPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración MenuItem
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Link).HasMaxLength(200);
                entity.Property(e => e.OpenMode).HasMaxLength(10);

                // Relación auto-referencial para jerarquía
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Order);
                entity.HasIndex(e => e.ParentId);
            });

            // Configuración Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configuración RoleMenuPermission
            modelBuilder.Entity<RoleMenuPermission>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.MenuPermissions)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.MenuItem)
                    .WithMany(m => m.RolePermissions)
                    .HasForeignKey(e => e.MenuItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice único para evitar duplicados
                entity.HasIndex(e => new { e.RoleId, e.MenuItemId }).IsUnique();
            });

            // Datos iniciales
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Inserta datos iniciales en la base de datos
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Roles por defecto
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Finanzas", Description = "Rol para área financiera", IsActive = true },
                new Role { Id = 2, Name = "Calidad", Description = "Rol para área de calidad", IsActive = true }
            );

            // Menús de ejemplo
            modelBuilder.Entity<MenuItem>().HasData(
                // Menús principales
                new MenuItem { Id = 1, Name = "Menú 1", Order = 1, IsActive = true },
                new MenuItem { Id = 2, Name = "Menú 2", Order = 2, IsActive = true },
                new MenuItem { Id = 3, Name = "Menú 3", Order = 3, IsActive = true },

                // Submenús
                new MenuItem { Id = 4, Name = "Submenú 1", ParentId = 1, Order = 1, Link = "https://www.google.com", OpenMode = "newtab", IsActive = true },
                new MenuItem { Id = 5, Name = "Submenú 2", ParentId = 1, Order = 2, Link = "https://www.microsoft.com", OpenMode = "redirect", IsActive = true },
                new MenuItem { Id = 6, Name = "Submenú 3", ParentId = 2, Order = 1, Link = "https://www.github.com", OpenMode = "newtab", IsActive = true },
                new MenuItem { Id = 7, Name = "Submenú 4", ParentId = 2, Order = 2, IsActive = true },
                new MenuItem { Id = 8, Name = "Submenú 5", ParentId = 3, Order = 1, IsActive = true }
            );

            // Permisos por defecto según el ejemplo
            modelBuilder.Entity<RoleMenuPermission>().HasData(
                // Finanzas: Menús 2 y 3 (y submenús 4 y 5)
                new RoleMenuPermission { Id = 1, RoleId = 1, MenuItemId = 2, IsActive = true },
                new RoleMenuPermission { Id = 2, RoleId = 1, MenuItemId = 3, IsActive = true },
                new RoleMenuPermission { Id = 3, RoleId = 1, MenuItemId = 7, IsActive = true }, // Submenú 4
                new RoleMenuPermission { Id = 4, RoleId = 1, MenuItemId = 8, IsActive = true }, // Submenú 5

                // Calidad: Menús 1 y 2 (y submenús 1 y 3)
                new RoleMenuPermission { Id = 5, RoleId = 2, MenuItemId = 1, IsActive = true },
                new RoleMenuPermission { Id = 6, RoleId = 2, MenuItemId = 2, IsActive = true },
                new RoleMenuPermission { Id = 7, RoleId = 2, MenuItemId = 4, IsActive = true }, // Submenú 1
                new RoleMenuPermission { Id = 8, RoleId = 2, MenuItemId = 6, IsActive = true }  // Submenú 3
            );
        }
    }
}