using System.ComponentModel.DataAnnotations;

namespace MenuManagement.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un rol del sistema
    /// </summary>
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navegación
        public virtual ICollection<RoleMenuPermission> MenuPermissions { get; set; } = new List<RoleMenuPermission>();

        /// <summary>
        /// Obtiene los menús activos para este rol
        /// </summary>
        public IEnumerable<MenuItem> GetActiveMenus()
        {
            return MenuPermissions
                .Where(rmp => rmp.IsActive && rmp.MenuItem.IsActive)
                .Select(rmp => rmp.MenuItem)
                .OrderBy(m => m.Order);
        }
    }
}