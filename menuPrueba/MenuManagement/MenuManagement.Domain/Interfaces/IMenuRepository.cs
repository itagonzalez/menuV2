using MenuManagement.Domain.Entities;

namespace MenuManagement.Domain.Interfaces;

public interface IMenuRepository
{
    Task<List<MenuItem>> GetMenuByRoleAsync(int roleId);
}