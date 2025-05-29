using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuManagement.Domain.Entities
{
    /// <summary>
    /// Entidad que representa un elemento de menú con jerarquía
    /// </summary>
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Link { get; set; }

        /// <summary>
        /// Modo de apertura: "redirect" o "newtab"
        /// </summary>
        [StringLength(10)]
        public string? OpenMode { get; set; }

        [NotMapped]
        public bool OpenInNewTab => OpenMode?.ToLower() == "newtab";


        public int Order { get; set; }

        /// <summary>
        /// ID del menú padre (null si es menú principal)
        /// </summary>
        public int? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }  // Opcional, nullable


        // Navegación
        [ForeignKey("ParentId")]
        public virtual MenuItem? Parent { get; set; }

        public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

        public virtual ICollection<RoleMenuPermission> RolePermissions { get; set; } = new List<RoleMenuPermission>();

        /// <summary>
        /// Obtiene la profundidad del menú en la jerarquía
        /// </summary>
        [NotMapped]
        public int Level
        {
            get
            {
                int level = 0;
                var current = Parent;
                while (current != null)
                {
                    level++;
                    current = current.Parent;
                }
                return level;
            }
        }

        /// <summary>
        /// Obtiene el path completo del menú
        /// </summary>
        [NotMapped]
        public string FullPath
        {
            get
            {
                var path = new List<string>();
                var current = this;
                while (current != null)
                {
                    path.Insert(0, current.Name);
                    current = current.Parent;
                }
                return string.Join(" > ", path);
            }
        }
    }
}