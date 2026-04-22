using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public interface IDeactiveEmployee
{
    Task<Result<object>> Execute(Guid id);
}