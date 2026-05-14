using AquaGas.Sale.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Sale.Application.UseCases;

public interface IGetById
{
    Task<Result<SaleResponse>> Execute(Guid id);
}