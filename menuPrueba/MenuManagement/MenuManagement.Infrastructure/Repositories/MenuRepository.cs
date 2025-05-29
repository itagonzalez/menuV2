using Microsoft.EntityFrameworkCore;
using MenuManagement.Domain.Entities;
using MenuManagement.Infrastructure.Data;

namespace MenuManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio para operaciones con menús y roles
    /// </summary>
    public class MenuRepository
    {
        private readonly AppDbContext _context;

        public MenuRepository(AppDbContext context)
        {
            _context = context;
        }

        #region MenuItem Operations

        /// <summary>
        /// Obtiene todos los elementos de menú con sus relaciones
        /// </summary>
        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Parent)
                .Include(m => m.Children)
                .Include(m => m.RolePermissions)
                .ThenInclude(rp => rp.Role)
                .OrderBy(m => m.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un elemento de menú por ID
        /// </summary>
        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Parent)
                .Include(m => m.Children)
                .Include(m => m.RolePermissions)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Obtiene los menús principales (sin padre)
        /// </summary>
        public async Task<List<MenuItem>> GetMainMenusAsync()
        {
            return await _context.MenuItems
                .Where(m => m.ParentId == null && m.IsActive)
                .Include(m => m.Children.Where(c => c.IsActive))
                .OrderBy(m => m.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene los menús para un rol específico
        /// </summary>
        public async Task<List<MenuItem>> GetMenusByRoleAsync(int roleId)
        {
            var roleMenus = await _context.RoleMenuPermissions
                .Where(rmp => rmp.RoleId == roleId && rmp.IsActive)
                .Include(rmp => rmp.MenuItem)
                .ThenInclude(m => m.Children)
                .Select(rmp => rmp.MenuItem)
                .Where(m => m.IsActive)
                .ToListAsync();

            // Construir jerarquía
            var result = new List<MenuItem>();
            var mainMenus = roleMenus.Where(m => m.ParentId == null).OrderBy(m => m.Order);

            foreach (var mainMenu in mainMenus)
            {
                var menuWithChildren = BuildMenuHierarchy(mainMenu, roleMenus);
                result.Add(menuWithChildren);
            }

            return result;
        }

        /// <summary>
        /// Construye la jerarquía de menús
        /// </summary>
        private MenuItem BuildMenuHierarchy(MenuItem menu, List<MenuItem> allMenus)
        {
            var children = allMenus
                .Where(m => m.ParentId == menu.Id)
                .OrderBy(m => m.Order)
                .ToList();

            menu.Children = children.Select(child => BuildMenuHierarchy(child, allMenus)).ToList();
            return menu;
        }

        /// <summary>
        /// Agrega un nuevo elemento de menú
        /// </summary>
        public async Task<MenuItem> AddMenuItemAsync(MenuItem menuItem)
        {
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        /// <summary>
        /// Actualiza un elemento de menú
        /// </summary>
        public async Task<MenuItem> UpdateMenuItemAsync(MenuItem menuItem)
        {
            _context.MenuItems.Update(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        /// <summary>
        /// Elimina un elemento de menú
        /// </summary>
        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null) return false;

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Role Operations

        /// <summary>
        /// Obtiene todos los roles
        /// </summary>
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Include(r => r.MenuPermissions)
                .ThenInclude(mp => mp.MenuItem)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un rol por ID
        /// </summary>
        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _context.Roles
                .Include(r => r.MenuPermissions)
                .ThenInclude(mp => mp.MenuItem)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Agrega un nuevo rol
        /// </summary>
        public async Task<Role> AddRoleAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        /// <summary>
        /// Actualiza un rol
        /// </summary>
        public async Task<Role> UpdateRoleAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return role;
        }

        /// <summary>
        /// Elimina un rol
        /// </summary>
        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Permission Operations

        /// <summary>
        /// Asigna un menú a un rol
        /// </summary>
        public async Task<bool> AssignMenuToRoleAsync(int roleId, int menuItemId, bool isActive = true)
        {
            var existing = await _context.RoleMenuPermissions
                .FirstOrDefaultAsync(rmp => rmp.RoleId == roleId && rmp.MenuItemId == menuItemId);

            if (existing != null)
            {
                existing.IsActive = isActive;
                _context.RoleMenuPermissions.Update(existing);
            }
            else
            {
                var permission = new RoleMenuPermission
                {
                    RoleId = roleId,
                    MenuItemId = menuItemId,
                    IsActive = isActive
                };
                _context.RoleMenuPermissions.Add(permission);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Remueve la asignación de un menú a un rol
        /// </summary>
        public async Task<bool> RemoveMenuFromRoleAsync(int roleId, int menuItemId)
        {
            var permission = await _context.RoleMenuPermissions
                .FirstOrDefaultAsync(rmp => rmp.RoleId == roleId && rmp.MenuItemId == menuItemId);

            if (permission == null) return false;

            _context.RoleMenuPermissions.Remove(permission);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Obtiene todos los permisos
        /// </summary>
        public async Task<List<RoleMenuPermission>> GetAllPermissionsAsync()
        {
            return await _context.RoleMenuPermissions
                .Include(rmp => rmp.Role)
                .Include(rmp => rmp.MenuItem)
                .ToListAsync();
        }

        #endregion
    }
}