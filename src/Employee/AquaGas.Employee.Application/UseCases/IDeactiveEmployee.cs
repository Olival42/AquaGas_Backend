using AquaGas.Shared.Results;

namespace AquaGas.Employee.Application.UseCases;

public interface IDeactiveEmployee
{
    Task<Result<object>> Execute(Guid id);
}