using AquaGas.Shared.Results;

namespace AquaGas.Auth.Application.UseCase;

public interface ILogout
{
    Task<Result> Execute(string refreshToken, string accessToken);
}