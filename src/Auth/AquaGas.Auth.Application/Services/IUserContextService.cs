using AquaGas.Auth.Domain.Enums;
using AquaGas.Shared.Results;

namespace AquaGas.Auth.Application.Services;

public interface IUserContextService
{
    Result<Guid> GetUserId();
    Result<string> GetUserName();
    Result<Role> GetRole();
}