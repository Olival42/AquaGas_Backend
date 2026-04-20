using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Auth.Application.UseCase;
public interface ILogin
{
    Task<Result<LoginResult>> Execute(LoginInput data);
}