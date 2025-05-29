using System.ComponentModel.DataAnnotations;

namespace MenuManagement.Domain.ViewModels
{
    /// <summary>
    /// ViewModel for Menu Item operations
    /// </summary>
    public class MenuItemViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(30, ErrorMessage = "El nombre no puede exceder 30 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "El link no puede exceder 200 caracteres")]
        [Url(ErrorMessage = "El formato del link no es válido")]
        public string? Link { get; set; }

        public string? OpenMode { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor a 0")]
        public int Order { get; set; }

        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public bool IsActive { get; set; } = true;
        public int Level { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public List<MenuItemViewModel> Children { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for Role operations
    /// </summary>
    public class RoleViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La descripción no puede exceder 200 caracteres")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int MenuCount { get; set; }
    }

    /// <summary>
    /// ViewModel for Permission management
    /// </summary>
    public class RolePermissionViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<MenuPermissionItem> MenuItems { get; set; } = new();
    }

    public class MenuPermissionItem
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool HasPermission { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// ViewModel for Demo page
    /// </summary>
    public class MenuDemoViewModel
    {
        public int SelectedRoleId { get; set; }
        public List<RoleViewModel> AvailableRoles { get; set; } = new();
        public List<MenuItemViewModel> MenuItems { get; set; } = new();
        public string SelectedRoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// API Response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> SuccessResult(T data, string message = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}