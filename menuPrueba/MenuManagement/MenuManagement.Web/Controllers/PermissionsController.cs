using Microsoft.AspNetCore.Mvc;
using MenuManagement.Domain.ViewModels;
using MenuManagement.Infrastructure.Repositories;

namespace MenuManagement.Web.Controllers
{
    /// <summary>
    /// Controlador para la gestión de permisos entre roles y menús
    /// </summary>
    public class PermissionsController : Controller
    {
        private readonly MenuRepository _menuRepository;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(MenuRepository menuRepository, ILogger<PermissionsController> logger)
        {
            _menuRepository = menuRepository;
            _logger = logger;
        }

        /// <summary>
        /// Página principal de gestión de permisos
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Cargando página de permisos");
                var roles = await _menuRepository.GetAllRolesAsync();
                var activeRoles = roles.Where(r => r.IsActive).ToList();

                var viewModel = new List<RolePermissionViewModel>();

                foreach (var role in activeRoles)
                {
                    var allMenus = await _menuRepository.GetAllMenuItemsAsync();
                    var roleMenus = role.MenuPermissions.Where(mp => mp.IsActive).Select(mp => mp.MenuItemId).ToHashSet();

                    var rolePermission = new RolePermissionViewModel
                    {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        MenuItems = allMenus.Where(m => m.IsActive).Select(m => new MenuPermissionItem
                        {
                            MenuItemId = m.Id,
                            MenuItemName = m.Name,
                            FullPath = m.FullPath,
                            Level = m.Level,
                            HasPermission = roleMenus.Contains(m.Id),
                            IsActive = true
                        }).OrderBy(m => m.FullPath).ToList()
                    };

                    viewModel.Add(rolePermission);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de permisos");
                TempData["Error"] = "Error al cargar los permisos";
                return View(new List<RolePermissionViewModel>());
            }
        }

        /// <summary>
        /// Página de gestión de permisos para un rol específico
        /// </summary>
        public async Task<IActionResult> Manage(int roleId)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var allMenus = await _menuRepository.GetAllMenuItemsAsync();
                var roleMenus = role.MenuPermissions.Where(mp => mp.IsActive).Select(mp => mp.MenuItemId).ToHashSet();

                var viewModel = new RolePermissionViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    MenuItems = allMenus.Where(m => m.IsActive).Select(m => new MenuPermissionItem
                    {
                        MenuItemId = m.Id,
                        MenuItemName = m.Name,
                        FullPath = m.FullPath,
                        Level = m.Level,
                        HasPermission = roleMenus.Contains(m.Id),
                        IsActive = true
                    }).OrderBy(m => m.FullPath).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar permisos del rol: {roleId}");
                TempData["Error"] = "Error al cargar los permisos del rol";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Procesa la actualización de permisos para un rol
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(int roleId, List<int> selectedMenus)
        {
            try
            {
                _logger.LogInformation($"Actualizando permisos para rol: {roleId}");

                var role = await _menuRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener todos los menús activos
                var allMenus = await _menuRepository.GetAllMenuItemsAsync();
                var activeMenuIds = allMenus.Where(m => m.IsActive).Select(m => m.Id).ToList();

                // Procesar cada menú activo
                foreach (var menuId in activeMenuIds)
                {
                    var shouldHavePermission = selectedMenus?.Contains(menuId) ?? false;
                    var currentPermission = role.MenuPermissions.FirstOrDefault(mp => mp.MenuItemId == menuId);

                    if (shouldHavePermission)
                    {
                        // Debe tener permiso
                        if (currentPermission == null)
                        {
                            // Crear nuevo permiso
                            await _menuRepository.AssignMenuToRoleAsync(roleId, menuId, true);
                        }
                        else if (!currentPermission.IsActive)
                        {
                            // Activar permiso existente
                            await _menuRepository.AssignMenuToRoleAsync(roleId, menuId, true);
                        }
                    }
                    else
                    {
                        // No debe tener permiso
                        if (currentPermission != null && currentPermission.IsActive)
                        {
                            // Desactivar permiso
                            await _menuRepository.AssignMenuToRoleAsync(roleId, menuId, false);
                        }
                    }
                }

                _logger.LogInformation($"Permisos actualizados para rol: {role.Name}");
                TempData["Success"] = "Permisos actualizados exitosamente";
                return RedirectToAction(nameof(Manage), new { roleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar permisos del rol: {roleId}");
                TempData["Error"] = "Error al actualizar los permisos";
                return RedirectToAction(nameof(Manage), new { roleId });
            }
        }

        /// <summary>
        /// API para alternar permiso específico (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TogglePermission(int roleId, int menuItemId)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Rol no encontrado" });
                }

                var currentPermission = role.MenuPermissions.FirstOrDefault(mp => mp.MenuItemId == menuItemId);
                bool newState = currentPermission?.IsActive != true;

                await _menuRepository.AssignMenuToRoleAsync(roleId, menuItemId, newState);

                _logger.LogInformation($"Permiso alternado - Rol: {roleId}, Menú: {menuItemId}, Estado: {newState}");

                return Json(new { success = true, hasPermission = newState });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al alternar permiso - Rol: {roleId}, Menú: {menuItemId}");
                return Json(new { success = false, message = "Error al cambiar permiso" });
            }
        }

        /// <summary>
        /// API para obtener resumen de permisos de un rol
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRolePermissionsSummary(int roleId)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Rol no encontrado" });
                }

                var menuItems = await _menuRepository.GetMenusByRoleAsync(roleId);
                var summary = new
                {
                    success = true,
                    roleId = role.Id,
                    roleName = role.Name,
                    totalMenus = role.MenuPermissions.Count(mp => mp.IsActive),
                    menuItems = menuItems.Select(m => new
                    {
                        id = m.Id,
                        name = m.Name,
                        fullPath = m.FullPath,
                        level = m.Level,
                        hasLink = !string.IsNullOrEmpty(m.Link),
                        link = m.Link,
                        openMode = m.OpenMode
                    }).ToList()
                };

                return Json(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener resumen de permisos del rol: {roleId}");
                return Json(new { success = false, message = "Error al obtener resumen" });
            }
        }

        /// <summary>
        /// Asignar permisos masivos a un rol
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BulkAssignPermissions(int roleId, string action)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Rol no encontrado" });
                }

                var allMenus = await _menuRepository.GetAllMenuItemsAsync();
                var activeMenuIds = allMenus.Where(m => m.IsActive).Select(m => m.Id).ToList();

                bool assignPermission = action.ToLower() == "grant";

                foreach (var menuId in activeMenuIds)
                {
                    await _menuRepository.AssignMenuToRoleAsync(roleId, menuId, assignPermission);
                }

                string actionText = assignPermission ? "asignados" : "removidos";
                _logger.LogInformation($"Permisos {actionText} masivamente para rol: {role.Name}");

                return Json(new
                {
                    success = true,
                    message = $"Permisos {actionText} exitosamente",
                    totalChanged = activeMenuIds.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en asignación masiva de permisos - Rol: {roleId}, Acción: {action}");
                return Json(new { success = false, message = "Error en la asignación masiva" });
            }
        }
    }
}