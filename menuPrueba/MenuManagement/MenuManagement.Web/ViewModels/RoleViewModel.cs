using System.ComponentModel.DataAnnotations;

namespace MenuManagement.Web.ViewModels;

public class RoleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "¡El nombre es obligatorio!")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string Name { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}