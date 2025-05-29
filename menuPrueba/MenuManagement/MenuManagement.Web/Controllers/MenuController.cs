using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuManagement.Infrastructure.Data;
using MenuManagement.Domain.Entities;
using MenuManagement.Domain.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace MenuManagement.Web.Controllers
{
    /// <summary>
    /// Controlador para la gestión de menús y submenús
    /// Implementa CRUD completo y funcionalidades dinámicas
    /// </summary>
    public class MenuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MenuController> _logger;

        public MenuController(AppDbContext context, ILogger<MenuController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Vista principal de gestión de menús
        /// </summary>
        /// <returns>Vista con la lista de menús principales</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Cargando vista principal de menús");

                var rootMenuItems = await _context.MenuItems
                    .Where(m => m.ParentId == null)
                    .OrderBy(m => m.Order)
                    .Include(m => m.Children)
                    .ToListAsync();

                ViewData["Title"] = "Gestión de Menús";
                ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/'>Inicio</a></li><li class='breadcrumb-item active'>Menús</li>";

                return View(rootMenuItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista de menús");
                TempData["ErrorMessage"] = "Error al cargar los menús";
                return View(new List<MenuItem>());
            }
        }

        /// <summary>
        /// Vista para crear un nuevo elemento de menú
        /// </summary>
        /// <param name="parentId">ID del menú padre (opcional)</param>
        /// <returns>Vista de creación</returns>
        public async Task<IActionResult> Create(int? parentId)
        {
            try
            {
                var viewModel = new MenuItemViewModel();

                if (parentId.HasValue)
                {
                    var parentItem = await _context.MenuItems.FindAsync(parentId.Value);
                    if (parentItem != null)
                    {
                        viewModel.ParentId = parentId.Value;
                        viewModel.ParentName = parentItem.Name;
                    }
                }

                // Cargar lista de posibles padres para el dropdown
                var availableParents = await _context.MenuItems
                    .Where(m => m.ParentId == null || m.Id != parentId)
                    .OrderBy(m => m.Name)
                    .Select(m => new { m.Id, m.Name })
                    .ToListAsync();

                ViewBag.AvailableParents = availableParents;
                ViewData["Title"] = "Crear Menú";
                ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/'>Inicio</a></li><li class='breadcrumb-item'><a href='/Menu'>Menús</a></li><li class='breadcrumb-item active'>Crear</li>";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista de creación");
                TempData["ErrorMessage"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Procesa la creación de un nuevo elemento de menú
        /// </summary>
        /// <param name="viewModel">Datos del menú a crear</param>
        /// <returns>Redirección según resultado</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItemViewModel viewModel)
        {
            try
            {
                // Validaciones personalizadas
                if (!string.IsNullOrEmpty(viewModel.Link) && !IsValidUrl(viewModel.Link))
                {
                    ModelState.AddModelError("Link", "El formato del enlace no es válido");
                }

                if (!string.IsNullOrEmpty(viewModel.Link) && string.IsNullOrEmpty(viewModel.OpenMode))
                {
                    ModelState.AddModelError("OpenMode", "Debe especificar el modo de apertura cuando hay un enlace");
                }

                if (ModelState.IsValid)
                {
                    // Verificar que el orden no esté duplicado en el mismo nivel
                    var duplicateOrder = await _context.MenuItems
                        .AnyAsync(m => m.ParentId == viewModel.ParentId && m.Order == viewModel.Order);

                    if (duplicateOrder)
                    {
                        ModelState.AddModelError("Order", "Ya existe un elemento con este orden en el mismo nivel");
                    }
                    else
                    {
                        var menuItem = new MenuItem
                        {
                            Name = viewModel.Name.Trim(),
                            Link = string.IsNullOrEmpty(viewModel.Link) ? null : viewModel.Link.Trim(),
                            OpenMode = string.IsNullOrEmpty(viewModel.Link) ? null : viewModel.OpenMode,
                            Order = viewModel.Order,
                            ParentId = viewModel.ParentId,
                            CreatedAt = DateTime.Now,
                            IsActive = true
                        };

                        _context.MenuItems.Add(menuItem);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Menú creado exitosamente: {menuItem.Name} (ID: {menuItem.Id})");
                        TempData["SuccessMessage"] = "Elemento de menú creado correctamente";

                        return RedirectToAction(nameof(Index));
                    }
                }

                // Recargar datos para la vista en caso de error
                var availableParents = await _context.MenuItems
                    .Where(m => m.ParentId == null)
                    .OrderBy(m => m.Name)
                    .Select(m => new { m.Id, m.Name })
                    .ToListAsync();

                ViewBag.AvailableParents = availableParents;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear elemento de menú");
                TempData["ErrorMessage"] = "Error al crear el elemento de menú";
                return View(viewModel);
            }
        }

        /// <summary>
        /// Vista para editar un elemento de menú existente
        /// </summary>
        /// <param name="id">ID del menú a editar</param>
        /// <returns>Vista de edición</returns>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    TempData["ErrorMessage"] = "Elemento de menú no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new MenuItemViewModel
                {
                    Id = menuItem.Id,
                    Name = menuItem.Name,
                    Link = menuItem.Link,
                    OpenMode = menuItem.OpenMode,
                    Order = menuItem.Order,
                    ParentId = menuItem.ParentId,
                    IsActive = menuItem.IsActive
                };

                // Cargar nombre del padre si existe
                if (menuItem.ParentId.HasValue)
                {
                    var parent = await _context.MenuItems.FindAsync(menuItem.ParentId.Value);
                    viewModel.ParentName = parent?.Name;
                }

                // Cargar lista de posibles padres (excluyendo el elemento actual y sus hijos)
                var availableParents = await GetAvailableParents(id);
                ViewBag.AvailableParents = availableParents;

                ViewData["Title"] = "Editar Menú";
                ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/'>Inicio</a></li><li class='breadcrumb-item'><a href='/Menu'>Menús</a></li><li class='breadcrumb-item active'>Editar</li>";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar menú para edición. ID: {id}");
                TempData["ErrorMessage"] = "Error al cargar el elemento de menú";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Procesa la edición de un elemento de menú
        /// </summary>
        /// <param name="id">ID del menú</param>
        /// <param name="viewModel">Datos actualizados</param>
        /// <returns>Redirección según resultado</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItemViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                TempData["ErrorMessage"] = "ID no coincide";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Validaciones personalizadas
                if (!string.IsNullOrEmpty(viewModel.Link) && !IsValidUrl(viewModel.Link))
                {
                    ModelState.AddModelError("Link", "El formato del enlace no es válido");
                }

                if (!string.IsNullOrEmpty(viewModel.Link) && string.IsNullOrEmpty(viewModel.OpenMode))
                {
                    ModelState.AddModelError("OpenMode", "Debe especificar el modo de apertura cuando hay un enlace");
                }

                if (ModelState.IsValid)
                {
                    // Verificar que el orden no esté duplicado en el mismo nivel
                    var duplicateOrder = await _context.MenuItems
                        .AnyAsync(m => m.ParentId == viewModel.ParentId &&
                                      m.Order == viewModel.Order &&
                                      m.Id != id);

                    if (duplicateOrder)
                    {
                        ModelState.AddModelError("Order", "Ya existe un elemento con este orden en el mismo nivel");
                    }
                    else
                    {
                        var menuItem = await _context.MenuItems.FindAsync(id);
                        if (menuItem == null)
                        {
                            TempData["ErrorMessage"] = "Elemento no encontrado";
                            return RedirectToAction(nameof(Index));
                        }

                        menuItem.Name = viewModel.Name.Trim();
                        menuItem.Link = string.IsNullOrEmpty(viewModel.Link) ? null : viewModel.Link.Trim();
                        menuItem.OpenMode = string.IsNullOrEmpty(viewModel.Link) ? null : viewModel.OpenMode;
                        menuItem.Order = viewModel.Order;
                        menuItem.ParentId = viewModel.ParentId;
                        menuItem.IsActive = viewModel.IsActive;
                        menuItem.UpdatedAt = DateTime.Now;

                        _context.Update(menuItem);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Menú actualizado exitosamente: {menuItem.Name} (ID: {menuItem.Id})");
                        TempData["SuccessMessage"] = "Elemento de menú actualizado correctamente";

                        return RedirectToAction(nameof(Index));
                    }
                }

                // Recargar datos para la vista
                var availableParents = await GetAvailableParents(id);
                ViewBag.AvailableParents = availableParents;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar menú. ID: {id}");
                TempData["ErrorMessage"] = "Error al actualizar el elemento de menú";
                return View(viewModel);
            }
        }

        /// <summary>
        /// Elimina un elemento de menú y todos sus hijos
        /// </summary>
        /// <param name="id">ID del menú a eliminar</param>
        /// <returns>Resultado JSON</returns>
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                var menuItem = await _context.MenuItems
                    .Include(m => m.Children)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (menuItem == null)
                {
                    return Json(new { success = false, message = "Elemento no encontrado" });
                }

                // Eliminar recursivamente todos los hijos
                await DeleteMenuItemRecursive(menuItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Menú eliminado exitosamente: {menuItem.Name} (ID: {menuItem.Id})");
                return Json(new { success = true, message = "Elemento eliminado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar menú. ID: {id}");
                return Json(new { success = false, message = "Error al eliminar el elemento" });
            }
        }

        /// <summary>
        /// Pantalla DEMO para mostrar menús dinámicos según rol
        /// </summary>
        /// <returns>Vista demo</returns>
        public async Task<IActionResult> Demo()
        {
            try
            {
                var roles = await _context.Roles
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                ViewData["Title"] = "Demo - Menú Dinámico";
                ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/'>Inicio</a></li><li class='breadcrumb-item'><a href='/Menu'>Menús</a></li><li class='breadcrumb-item active'>Demo</li>";

                return View(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista demo");
                TempData["ErrorMessage"] = "Error al cargar la vista demo";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API para obtener menús disponibles para un rol específico
        /// </summary>
        /// <param name="roleId">ID del rol</param>
        /// <returns>JSON con estructura de menús</returns>
        [HttpGet]
        public async Task<JsonResult> GetMenuForRole(int roleId)
        {
            try
            {
                var menuItems = await _context.RoleMenuPermissions
                    .Where(rmp => rmp.RoleId == roleId && rmp.IsActive)
                    .Include(rmp => rmp.MenuItem)
                    .Select(rmp => rmp.MenuItem)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.Order)
                    .ToListAsync();

                var hierarchicalMenus = BuildMenuHierarchy(menuItems);

                return Json(hierarchicalMenus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar menús para rol. RoleId: {roleId}");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// API para obtener elementos de menú raíz
        /// </summary>
        /// <returns>JSON con menús principales</returns>
        [HttpGet]
        public async Task<JsonResult> GetRootItems()
        {
            try
            {
                var rootItems = await _context.MenuItems
                    .Where(m => m.ParentId == null && m.IsActive)
                    .OrderBy(m => m.Order)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.Link,
                        m.OpenMode,
                        m.Order,
                        HasChildren = m.Children.Any()
                    })
                    .ToListAsync();

                return Json(rootItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar elementos raíz");
                return Json(new List<object>());
            }
        }

        /// <summary>
        /// API para obtener elementos hijos de un menú
        /// </summary>
        /// <param name="parentId">ID del menú padre</param>
        /// <returns>JSON con menús hijos</returns>
        [HttpGet]
        public async Task<JsonResult> GetChildren(int parentId)
        {
            try
            {
                var children = await _context.MenuItems
                    .Where(m => m.ParentId == parentId && m.IsActive)
                    .OrderBy(m => m.Order)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.Link,
                        m.OpenMode,
                        m.Order,
                        HasChildren = m.Children.Any()
                    })
                    .ToListAsync();

                return Json(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar elementos hijos. ParentId: {parentId}");
                return Json(new List<object>());
            }
        }

        #region Private Methods

        /// <summary>
        /// Valida si una URL tiene formato correcto
        /// </summary>
        /// <param name="url">URL a validar</param>
        /// <returns>True si es válida</returns>
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _) ||
                   url.StartsWith("/") ||
                   url.StartsWith("~/") ||
                   url.StartsWith("#");
        }

        /// <summary>
        /// Obtiene lista de padres disponibles para un menú (excluyendo el elemento y sus hijos)
        /// </summary>
        /// <param name="excludeId">ID a excluir de la lista</param>
        /// <returns>Lista de padres disponibles</returns>
        private async Task<List<object>> GetAvailableParents(int excludeId)
        {
            // Obtener todos los IDs de descendientes del elemento a excluir
            var descendantIds = await GetDescendantIds(excludeId);
            descendantIds.Add(excludeId);

            var availableParents = await _context.MenuItems
                .Where(m => !descendantIds.Contains(m.Id))
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync();

            return availableParents.Cast<object>().ToList();
        }

        /// <summary>
        /// Obtiene todos los IDs de descendientes de un elemento
        /// </summary>
        /// <param name="parentId">ID del padre</param>
        /// <returns>Lista de IDs descendientes</returns>
        private async Task<List<int>> GetDescendantIds(int parentId)
        {
            var descendants = new List<int>();
            var children = await _context.MenuItems
                .Where(m => m.ParentId == parentId)
                .Select(m => m.Id)
                .ToListAsync();

            descendants.AddRange(children);

            foreach (var childId in children)
            {
                var grandChildren = await GetDescendantIds(childId);
                descendants.AddRange(grandChildren);
            }

            return descendants;
        }

        /// <summary>
        /// Elimina recursivamente un elemento de menú y todos sus hijos
        /// </summary>
        /// <param name="menuItem">Elemento a eliminar</param>
        private async Task DeleteMenuItemRecursive(MenuItem menuItem)
        {
            // Cargar hijos si no están cargados
            if (!_context.Entry(menuItem).Collection(m => m.Children).IsLoaded)
            {
                await _context.Entry(menuItem).Collection(m => m.Children).LoadAsync();
            }

            // Eliminar recursivamente todos los hijos
            foreach (var child in menuItem.Children.ToList())
            {
                await DeleteMenuItemRecursive(child);
            }

            // Eliminar permisos asociados
            var permissions = await _context.RoleMenuPermissions
                .Where(rmp => rmp.MenuItemId == menuItem.Id)
                .ToListAsync();

            _context.RoleMenuPermissions.RemoveRange(permissions);

            // Eliminar el elemento
            _context.MenuItems.Remove(menuItem);
        }

        /// <summary>
        /// Construye una estructura jerárquica de menús
        /// </summary>
        /// <param name="flatMenuItems">Lista plana de elementos</param>
        /// <returns>Estructura jerárquica</returns>
        private List<object> BuildMenuHierarchy(List<MenuItem> flatMenuItems)
        {
            var menuDict = flatMenuItems.ToDictionary(m => m.Id, m => new
            {
                m.Id,
                m.Name,
                m.Link,
                m.OpenMode,
                m.Order,
                m.ParentId,
                Children = new List<object>()
            });

            var rootMenus = new List<object>();

            foreach (var item in menuDict.Values.OrderBy(m => m.Order))
            {
                if (item.ParentId == null)
                {
                    rootMenus.Add(item);
                }
                else if (menuDict.ContainsKey(item.ParentId.Value))
                {
                    ((List<object>)menuDict[item.ParentId.Value].Children).Add(item);
                }
            }

            return rootMenus;
        }

        #endregion
    }
}