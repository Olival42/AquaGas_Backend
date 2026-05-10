using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Domain.Models;

namespace AquaGas.Auth.Application.Services;

public interface IJwtService
{
    TokenResult GenerateAccessToken(User user);
    TokenResult GenerateRefreshToken(User user);
    DateTime GetTokenExpiration(string token);
    Guid? GetUserId(string token);
}