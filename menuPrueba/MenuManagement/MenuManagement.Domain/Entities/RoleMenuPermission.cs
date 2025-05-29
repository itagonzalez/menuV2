using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuManagement.Domain.Entities
{
    /// <summary>
    /// Entidad que relaciona roles con elementos de menú
    /// </summary>
    public class RoleMenuPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navegación
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; } = null!;
    }
}