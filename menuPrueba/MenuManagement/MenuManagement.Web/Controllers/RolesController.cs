using Microsoft.AspNetCore.Mvc;
using MenuManagement.Domain.Entities;
using MenuManagement.Domain.ViewModels;
using MenuManagement.Infrastructure.Repositories;

namespace MenuManagement.Web.Controllers
{
    /// <summary>
    /// Controlador para la gestión de roles
    /// </summary>
    public class RolesController : Controller
    {
        private readonly MenuRepository _menuRepository;
        private readonly ILogger<RolesController> _logger;

        public RolesController(MenuRepository menuRepository, ILogger<RolesController> logger)
        {
            _menuRepository = menuRepository;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los roles
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de roles");
                var roles = await _menuRepository.GetAllRolesAsync();
                var viewModel = roles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    MenuCount = r.MenuPermissions.Count(mp => mp.IsActive)
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de roles");
                TempData["Error"] = "Error al cargar los roles";
                return View(new List<RoleViewModel>());
            }
        }

        /// <summary>
        /// Formulario para crear nuevo rol
        /// </summary>
        public IActionResult Create()
        {
            return View(new RoleViewModel());
        }

        /// <summary>
        /// Procesa la creación de un nuevo rol
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var role = new Role
                    {
                        Name = model.Name,
                        Description = model.Description,
                        IsActive = model.IsActive
                    };

                    await _menuRepository.AddRoleAsync(role);
                    _logger.LogInformation($"Rol creado: {role.Name}");
                    TempData["Success"] = "Rol creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol");
                TempData["Error"] = "Error al crear el rol";
                return View(model);
            }
        }

        /// <summary>
        /// Formulario para editar rol
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(id);
                if (role == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new RoleViewModel
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar rol para editar: {id}");
                TempData["Error"] = "Error al cargar el rol";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Procesa la edición de un rol
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoleViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["Error"] = "ID de rol no válido";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    var role = await _menuRepository.GetRoleByIdAsync(id);
                    if (role == null)
                    {
                        TempData["Error"] = "Rol no encontrado";
                        return RedirectToAction(nameof(Index));
                    }

                    role.Name = model.Name;
                    role.Description = model.Description;
                    role.IsActive = model.IsActive;

                    await _menuRepository.UpdateRoleAsync(role);
                    _logger.LogInformation($"Rol actualizado: {role.Name}");
                    TempData["Success"] = "Rol actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar rol: {id}");
                TempData["Error"] = "Error al actualizar el rol";
                return View(model);
            }
        }

        /// <summary>
        /// Elimina un rol
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _menuRepository.DeleteRoleAsync(id);
                if (result)
                {
                    _logger.LogInformation($"Rol eliminado: {id}");
                    TempData["Success"] = "Rol eliminado exitosamente";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar el rol";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar rol: {id}");
                TempData["Error"] = "Error al eliminar el rol. Verifica que no tenga permisos asignados.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Detalle de un rol con sus menús asignados
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(id);
                if (role == null)
                {
                    TempData["Error"] = "Rol no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var menuItems = await _menuRepository.GetMenusByRoleAsync(id);

                var viewModel = new
                {
                    Role = new RoleViewModel
                    {
                        Id = role.Id,
                        Name = role.Name,
                        Description = role.Description,
                        IsActive = role.IsActive,
                        MenuCount = role.MenuPermissions.Count(mp => mp.IsActive)
                    },
                    MenuItems = menuItems.Select(m => new MenuItemViewModel
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Link = m.Link,
                        OpenMode = m.OpenMode,
                        Order = m.Order,
                        Level = m.Level,
                        FullPath = m.FullPath,
                        IsActive = m.IsActive,
                        Children = MapMenuChildren(m.Children)
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar detalles del rol: {id}");
                TempData["Error"] = "Error al cargar los detalles del rol";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API para cambiar estado activo de un rol
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var role = await _menuRepository.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Rol no encontrado" });
                }

                role.IsActive = !role.IsActive;
                await _menuRepository.UpdateRoleAsync(role);

                _logger.LogInformation($"Estado de rol cambiado: {role.Name} - Activo: {role.IsActive}");

                return Json(new { success = true, isActive = role.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cambiar estado del rol: {id}");
                return Json(new { success = false, message = "Error al cambiar estado" });
            }
        }

        /// <summary>
        /// Mapea hijos de menú recursivamente
        /// </summary>
        private List<MenuItemViewModel> MapMenuChildren(ICollection<MenuItem> children)
        {
            return children.Select(c => new MenuItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Link = c.Link,
                OpenMode = c.OpenMode,
                Order = c.Order,
                Level = c.Level,
                FullPath = c.FullPath,
                IsActive = c.IsActive,
                Children = MapMenuChildren(c.Children)
            }).OrderBy(c => c.Order).ToList();
        }
    }
}