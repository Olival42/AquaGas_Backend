namespace AquaGas.Api.Modules.Auth.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Shared.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
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
            .FirstOrDefaultAsync(u => u.UserName == match && u.IsActive);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<bool> AnyByUserNameAsync(string userName)
    {
        var parsed = UserName.Create(userName);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Users
            .AnyAsync(u => u.UserName == match);
    }

    public async Task<bool> AnyByUserNameAsync(string userName, Guid ignoreUserId)
    {
        var parsed = UserName.Create(userName);
        if (parsed.IsFailure || parsed.Value is null)
            return false;

        var match = parsed.Value;

        return await _context.Users
            .AnyAsync(u =>
                u.UserName == match &&
                u.Id != ignoreUserId);
    }

}
