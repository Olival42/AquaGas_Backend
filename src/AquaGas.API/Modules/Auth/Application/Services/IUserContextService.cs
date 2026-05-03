using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Auth.Application.Services;

public interface IUserContextService
{
    Result<Guid> GetUserId();
    Result<string> GetUserName();
}