using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Auth.Application.UseCase;

public interface ILogout
{
    Task<Result> Execute(string refreshToken, string accessToken);
}