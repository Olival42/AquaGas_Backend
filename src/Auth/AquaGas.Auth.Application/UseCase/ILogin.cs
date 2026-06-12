using AquaGas.Auth.Application.Dtos.Requests;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Auth.Application.UseCase;
public interface ILogin
{
    Task<Result<LoginResult>> Execute(LoginInput data);
}