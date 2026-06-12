using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.Services;

namespace AquaGas.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _hasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher hasher)
    {
        _userRepository = userRepository;
        _hasher = hasher;
    }

    public async Task<User?> Authenticate(string userName, string password)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);

        if (user == null)
            return null;

        if (!_hasher.Verify(user.PasswordHash, password))
            return null;

        return user;
    }
}