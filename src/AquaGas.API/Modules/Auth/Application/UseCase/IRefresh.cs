using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Auth.Application.UseCase;

public interface IRefresh
{
    Task<Result<RefreshResult>> Execute(string refreshToken);
}