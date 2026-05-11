using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AquaGas.Auth.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        var parsed = UserName.Create(userName);
        if (!parsed.IsSuccess || parsed.Value is null)
            return null;

        UserName match = parsed.Value;

        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserName.Value == match.Value && u.IsActive);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<User?> GetByEmployeeIdAsync(Guid employeeId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.IsActive);
    }

    public async Task<User?> GetByEmployeeIdAsync(Guid employeeId, bool onlyActive = true)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && (!onlyActive || u.IsActive));
    }

    public async Task<bool> AnyByUserNameAsync(string userName)
    {
        var parsed = UserName.Create(userName);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Users
            .AnyAsync(u => u.UserName.Value == match.Value && u.IsActive);
    }

    public async Task<bool> AnyByUserNameAsync(string userName, Guid ignoreUserId)
    {
        var parsed = UserName.Create(userName);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Users
            .AnyAsync(u =>
                u.UserName.Value == match.Value &&
                u.Id != ignoreUserId &&
                u.IsActive);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
