using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Auth.Application.UseCase;

public interface IRefresh
{
    Task<Result<RefreshResult>> Execute(string refreshToken);
}