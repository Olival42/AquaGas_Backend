using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.Models;

namespace AquaGas.Api.Modules.Auth.Application.Services;


public interface IJwtService
{
    TokenResult GenerateAccessToken(User user);
    TokenResult GenerateRefreshToken(User user);
    DateTime GetTokenExpiration(string token);
    Guid? GetUserId(string token);
}